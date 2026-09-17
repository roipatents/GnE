# Changelog

## 1.0.6 - 2026-09-17

- Restore the storyboard-compatible HTML value-transformer registration and remove a stale outlet so the main controller loads instead of leaving a blank window.
- Use an explicit private URL scheme for in-app prompt links so Choose and Run commands are handled by GnE while external links remain with macOS.
- Require release smoke tests to observe the GnE controls and confirm that the Choose link opens the source spreadsheet picker before accepting a package.
- Create the installer only after the final signed app is stable, preventing post-signing bundle changes from invalidating the app or package.
- Sign with the generated .NET JIT entitlement and remove a hidden marker from the entitlement source so the installed app retains a valid signature.

## 1.0.5 - 2026-09-15

- Activate GnE explicitly at launch and bring its storyboard window to the front.
- Prevent managed or background launches from appearing to crash while the application remains open behind another app.
- Build with the current .NET 10.0.400 SDK feature band.

## 1.0.4 - 2026-08-05

- Build the macOS release with Microsoft's relocatable .NET runtime instead of Homebrew's runtime pack.
- Reject packaged native libraries that depend on Homebrew or other non-system absolute paths.
- Launch-test the signed application before notarizing and publishing the installer.

## 1.0.3 - 2026-08-04

- Rebuild the macOS application for Apple silicon on .NET 10.
- Upgrade to EPPlus 8 with commercial licensing injected from 1Password for Richardson Oliver distributions.
- Update the macOS bindings and nullable handling for the current SDK without build warnings.
- Correct the MIT license holder and align the application and WIPO data notices with their upstream MIT licenses.
- Produce a Developer ID-signed installer with an explicit notarization and stapling workflow.
- Correct the repository deployment metadata so the signed public release can be managed through JAMF.

## 1.0.2 - 2025-08-18

- Correct links and package the Apple-silicon macOS application.
