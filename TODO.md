# GnE signed package repair

- [x] Establish proposed release version 1.0.3 from current `main`.
- [x] Upgrade the public project to .NET 10 and compatible public package references.
- [x] Correct the public, self-contained macOS packaging and metadata workflow.
- [x] Build and test the arm64 application with the current Xcode toolchain.
- [x] Sign the app and installer with the ROL Developer ID identities.
- [x] Notarize and staple the installer, then verify Gatekeeper acceptance.
- [x] Prepare a no-publication GitHub/JAMF replacement preview for approval.

## Discovered items

- Existing `v1.0.2` package signature is invalid and the tag predates substantial changes now on `main`.
- The public project must not consume the private `ROI.BuildActions` package feed.
- EPPlus 8 uses the purchased commercial key injected from 1Password into an ignored build input; the licensed configuration may be distributed in the package but is not stored in Git.
- `System.CommandLine` remains on its existing beta because the stable 2.0 API requires a separate CLI source migration; the macOS release asset does not include the command-line project.
