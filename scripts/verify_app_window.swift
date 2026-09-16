import AppKit
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
