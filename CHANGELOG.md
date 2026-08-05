# Changelog

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
