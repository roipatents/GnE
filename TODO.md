# GnE release work

- [ ] Build and locally verify the GnE 1.0.6 release installer. (in progress)
  - [x] Reject the 1.0.5 candidate after visual testing found a blank application window.
  - [x] Trace the blank window to a case-sensitive value-transformer rename in `v1.0.2` and a stale storyboard outlet.
  - [x] Restore the storyboard-compatible transformer registration and remove the stale outlet.
  - [x] Restore the Choose and Run prompt links with an app-specific command URL scheme.
  - [x] Strengthen launch validation to require the GnE outline and Next button, then open and dismiss the source spreadsheet picker through the Choose link.
  - [x] Reject the stale 1.0.5 installer attempt after macOS detected that the package changed while Installer had it open.
  - [x] Make package creation follow the final application signing step so the installer payload remains stable.
  - [x] Correct the malformed entitlement source and require the final signature to use the generated .NET JIT entitlement.
  - [x] Build and sign a fresh installer, then validate the package and extracted app before installation.
  - [ ] Notarize, staple, and run the final Gatekeeper assessment.
  - [x] Install the newly built package.
  - [x] Verify the installed version, receipt, signature stability, process stability, visible controls, and Choose-link file picker.

- [x] Evaluate the GnE 1.0.5 launch fix on macOS 27 and supersede it with 1.0.6 after visual testing found the blank UI.
  - [x] Confirm the previous release remains alive and creates its main window instead of producing a crash report.
  - [x] Explicitly activate the application and bring the main storyboard window forward at launch.
  - [x] Strengthen launch validation to require a visible, active main window.
  - [x] Update the .NET SDK pin to the 10.0.400 feature band.
  - [x] Build and test on macOS 26.6.2.
  - [x] Build, sign, notarize, staple, and publish the 1.0.5 installer.
  - [x] Commit and push the validated 1.0.5 source and release tag.

- [x] Repair the native-library signing failure in GnE 1.0.3 as GnE 1.0.4.
  - [x] Diagnose the launch failure as a non-system Brotli dependency in the bundled .NET compression library.
  - [x] Default release packaging to the official Microsoft .NET SDK and reject non-system native dependencies.
  - [x] Vendor the public-safe macOS version synchronization, Brotli repair, and native-dependency validation logic required by this repository.
  - [x] Build, test, sign, notarize, staple, launch-verify, and publish the 1.0.4 release.

- [x] Upgrade the public project to .NET 10 and compatible public package references.
- [x] Correct the public, self-contained macOS packaging and metadata workflow.
- [x] Build and test the arm64 application with the current Xcode toolchain.
- [x] Add signed-app launch validation and release integrity checks.

## Discovered items

- The existing `v1.0.2` package signature is invalid, and the tag predates substantial changes now on `main`.
- The public project contains its release-specific macOS build logic directly in the repository.
- EPPlus 8 requires a commercial license for release builds; licensed configuration is supplied outside Git and is not stored in this repository.
- `System.CommandLine` remains on its existing beta because the stable 2.0 API requires a separate CLI source migration; the macOS release asset does not include the command-line project.
- Release packaging uses the official .NET SDK to avoid non-system native dependencies that are incompatible with the hardened runtime.
