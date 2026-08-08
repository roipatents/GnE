# Vendored ROI.BuildActions macOS build logic

GnE vendors the public-safe macOS version, Homebrew Brotli repair, and native
Mach-O dependency validation logic from the private `ROI.BuildActions` NuGet
package. This allows the public repository to build without credentials for
Richardson Oliver's private GitHub Packages feed.

- Package: `ROI.BuildActions` 1.1.4
- Tag: `build-actions-v1.1.4`
- Source commit: `7ebb77ecc3f92da24068467ff88db569b51a9f39`
- Included: project-derived intermediate `Info.plist` version metadata and
  finished-bundle validation; Homebrew Brotli closure relocation; Brotli MIT
  notice placement; signing repair; and final native-dependency validation
- Excluded: 1Password injection, artifact collection, organization signing
  presets, installer construction, notarization submission, and private-feed
  configuration

The vendored targets retain their upstream target and property names so the
files can be compared against a later public package. Replace this directory
with a `PackageReference` when `ROI.BuildActions` is available from a public
NuGet source.
