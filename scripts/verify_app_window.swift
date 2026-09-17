import AppKit
import ApplicationServices
import CoreGraphics

guard CommandLine.arguments.count == 2,
      let processIdentifier = Int32(CommandLine.arguments[1]) else {
    fputs("Usage: verify_app_window.swift <process-id>\n", stderr)
    exit(64)
}

guard let application = NSRunningApplication(processIdentifier: processIdentifier) else {
    fputs("No running application was found for process \(processIdentifier).\n", stderr)
    exit(1)
}

let windowInformation = CGWindowListCopyWindowInfo(
    [.optionOnScreenOnly, .excludeDesktopElements],
    kCGNullWindowID
) as? [[String: Any]] ?? []

let hasPresentedWindow = windowInformation.contains { window in
    guard let owner = window[kCGWindowOwnerPID as String] as? Int32,
          owner == processIdentifier,
          let layer = window[kCGWindowLayer as String] as? Int,
          layer == 0,
          let boundsValue = window[kCGWindowBounds as String],
          let bounds = CGRect(dictionaryRepresentation: boundsValue as! CFDictionary) else {
        return false
    }

    let alpha = window[kCGWindowAlpha as String] as? Double ?? 1
    return bounds.width > 0 && bounds.height > 0 && alpha > 0
}

guard hasPresentedWindow else {
    fputs("GnE is running but has no visible application window.\n", stderr)
    exit(1)
}

guard application.isActive else {
    fputs("GnE created a window but did not become the active application.\n", stderr)
    exit(1)
}

func attribute<T>(_ element: AXUIElement, _ name: CFString, as type: T.Type) -> T? {
    var value: CFTypeRef?
    guard AXUIElementCopyAttributeValue(element, name, &value) == .success else {
        return nil
    }
    return value as? T
}

func containsRequiredControls(_ element: AXUIElement, depth: Int = 0) -> (outline: Bool, nextButton: Bool) {
    guard depth < 20 else {
        return (false, false)
    }

    let role = attribute(element, kAXRoleAttribute as CFString, as: String.self)
    let title = attribute(element, kAXTitleAttribute as CFString, as: String.self)
    var result = (
        outline: role == kAXOutlineRole as String,
        nextButton: role == kAXButtonRole as String && title == "Next"
    )

    let children = attribute(element, kAXChildrenAttribute as CFString, as: [AXUIElement].self) ?? []
    for child in children {
        let childResult = containsRequiredControls(child, depth: depth + 1)
        result.outline = result.outline || childResult.outline
        result.nextButton = result.nextButton || childResult.nextButton
        if result.outline && result.nextButton {
            break
        }
    }
    return result
}

func containsText(_ element: AXUIElement, _ expected: String, depth: Int = 0) -> Bool {
    guard depth < 20 else {
        return false
    }

    for name in [kAXTitleAttribute, kAXValueAttribute, kAXDescriptionAttribute] {
        if let value = attribute(element, name as CFString, as: String.self),
           value.contains(expected) {
            return true
        }
    }

    let children = attribute(element, kAXChildrenAttribute as CFString, as: [AXUIElement].self) ?? []
    return children.contains { containsText($0, expected, depth: depth + 1) }
}

func findElement(
    _ element: AXUIElement,
    role expectedRole: String,
    containingText expectedText: String,
    depth: Int = 0
) -> AXUIElement? {
    guard depth < 20 else {
        return nil
    }

    let role = attribute(element, kAXRoleAttribute as CFString, as: String.self)
    if role == expectedRole && containsText(element, expectedText) {
        return element
    }

    let children = attribute(element, kAXChildrenAttribute as CFString, as: [AXUIElement].self) ?? []
    for child in children {
        if let match = findElement(child, role: expectedRole, containingText: expectedText, depth: depth + 1) {
            return match
        }
    }
    return nil
}

func waitForElement(
    in root: AXUIElement,
    role: String,
    containingText text: String,
    timeout: TimeInterval = 5
) -> AXUIElement? {
    let deadline = Date().addingTimeInterval(timeout)
    repeat {
        if let element = findElement(root, role: role, containingText: text) {
            return element
        }
        Thread.sleep(forTimeInterval: 0.25)
    } while Date() < deadline
    return nil
}

func activate(_ element: AXUIElement) -> Bool {
    if AXUIElementPerformAction(element, kAXPressAction as CFString) == .success {
        return true
    }

    guard let positionValue = attribute(element, kAXPositionAttribute as CFString, as: AXValue.self),
          let sizeValue = attribute(element, kAXSizeAttribute as CFString, as: AXValue.self) else {
        return false
    }

    var position = CGPoint.zero
    var size = CGSize.zero
    guard AXValueGetValue(positionValue, .cgPoint, &position),
          AXValueGetValue(sizeValue, .cgSize, &size) else {
        return false
    }

    let clickPoint = CGPoint(x: position.x + size.width / 2, y: position.y + size.height / 2)
    guard let mouseDown = CGEvent(
        mouseEventSource: nil,
        mouseType: .leftMouseDown,
        mouseCursorPosition: clickPoint,
        mouseButton: .left
    ), let mouseUp = CGEvent(
        mouseEventSource: nil,
        mouseType: .leftMouseUp,
        mouseCursorPosition: clickPoint,
        mouseButton: .left
    ) else {
        return false
    }

    mouseDown.post(tap: .cghidEventTap)
    mouseUp.post(tap: .cghidEventTap)
    return true
}

let accessibilityApplication = AXUIElementCreateApplication(processIdentifier)
let deadline = Date().addingTimeInterval(5)
var controls = (outline: false, nextButton: false)
repeat {
    controls = containsRequiredControls(accessibilityApplication)
    if controls.outline && controls.nextButton {
        break
    }
    Thread.sleep(forTimeInterval: 0.25)
} while Date() < deadline

guard controls.outline && controls.nextButton else {
    fputs("GnE created a window but its main outline and Next button are not accessible.\n", stderr)
    exit(1)
}

guard let sourceSpreadsheetRow = waitForElement(
    in: accessibilityApplication,
    role: kAXRowRole as String,
    containingText: "Open Source Spreadsheet"
) else {
    fputs("GnE's Open Source Spreadsheet step is not accessible.\n", stderr)
    exit(1)
}

let selectStepResult = AXUIElementPerformAction(sourceSpreadsheetRow, kAXPressAction as CFString)
if selectStepResult != .success {
    guard AXUIElementSetAttributeValue(
        sourceSpreadsheetRow,
        kAXSelectedAttribute as CFString,
        kCFBooleanTrue
    ) == .success else {
        fputs("GnE's Open Source Spreadsheet step could not be selected.\n", stderr)
        exit(1)
    }
}

guard let chooseLink = waitForElement(
    in: accessibilityApplication,
    role: "AXLink",
    containingText: "Choose"
) else {
    fputs("GnE's Choose prompt is not exposed as an actionable link.\n", stderr)
    exit(1)
}

guard activate(chooseLink) else {
    fputs("GnE's Choose link could not be activated.\n", stderr)
    exit(1)
}

guard waitForElement(
    in: accessibilityApplication,
    role: kAXWindowRole as String,
    containingText: "Choose Source Spreadsheet"
) != nil else {
    fputs("GnE's Choose link did not open the source spreadsheet picker.\n", stderr)
    exit(1)
}

guard let cancelButton = waitForElement(
    in: accessibilityApplication,
    role: kAXButtonRole as String,
    containingText: "Cancel"
) else {
    fputs("GnE's source spreadsheet picker did not expose a Cancel button.\n", stderr)
    exit(1)
}

guard activate(cancelButton) else {
    fputs("GnE's source spreadsheet picker could not be dismissed.\n", stderr)
    exit(1)
}
