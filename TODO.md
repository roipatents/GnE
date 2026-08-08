# GnE signed package repair

- [ ] Repair the GnE 1.0.3 launch failure found during the JAMF pilot. (in progress)
  - [x] Diagnose the crash as a Homebrew-linked Brotli dependency in the bundled .NET compression library.
  - [x] Default release packaging to the official Microsoft .NET SDK and reject non-system native dependencies.
  - [x] Vendor the public-safe ROI.BuildActions 1.1.4 version synchronization, Brotli repair, and native-dependency validation logic without private-feed or secret-injection configuration.
  - [x] Bump to 1.0.4 with release notes and add a signed-app launch smoke test.
  - [x] Rebuild, test, sign, notarize, staple, and launch-verify 1.0.4 with vendored Build Actions 1.1.4.
  - [ ] Publish an immutable 1.0.4 release and replace 1.0.3 in the all-computers JAMF upgrade and enrollment policies.

- [x] Establish proposed release version 1.0.3 from current `main`.
- [x] Restore the standard `rol-profile` notarization keychain default after creating and validating the matching local profile.
- [x] Upgrade the public project to .NET 10 and compatible public package references.
- [x] Correct the public, self-contained macOS packaging and metadata workflow.
- [x] Build and test the arm64 application with the current Xcode toolchain.
- [x] Sign the app and installer with the ROL Developer ID identities.
- [x] Notarize and staple the installer, then verify Gatekeeper acceptance.
- [x] Prepare a no-publication GitHub/JAMF replacement preview for approval.

## Discovered items

- Existing `v1.0.2` package signature is invalid and the tag predates substantial changes now on `main`.
- The public project does not consume the private `ROI.BuildActions` package feed; its selected public-safe 1.1.4 macOS version and dependency logic is vendored with exact source provenance.
- EPPlus 8 uses the purchased commercial key injected from 1Password into an ignored build input; the licensed configuration may be distributed in the package but is not stored in Git.
- `System.CommandLine` remains on its existing beta because the stable 2.0 API requires a separate CLI source migration; the macOS release asset does not include the command-line project.
- GnE 1.0.3 installs successfully through JAMF but cannot launch: Homebrew's .NET 10 runtime pack links `libSystem.IO.Compression.Native.dylib` to Homebrew Brotli libraries, which hardened runtime rejects because they have a different signing Team ID.
- Resolved build-environment dependency: the official SDK now owns the macOS 26.5 workload, so release packaging no longer needs user-workload-root overrides.
