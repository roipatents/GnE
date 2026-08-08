#!/bin/zsh
set -euo pipefail

repo_root="${0:A:h:h}"
version="$(sed -n '1{s/\r$//;p;q;}' "$repo_root/VERSION")"
project="$repo_root/src/GenderNameEstimator.UI.Mac/GenderNameEstimator.UI.Mac.csproj"
solution="$repo_root/GenderNameEstimator.sln"
application_identity="${APPLICATION_IDENTITY:-Developer ID Application: Richardson Oliver Law Group LLP (2B7MH5Z594)}"
installer_identity="${INSTALLER_IDENTITY:-Developer ID Installer: Richardson Oliver Law Group LLP (2B7MH5Z594)}"
notary_profile="${NOTARY_PROFILE:-Notary}"
artifacts_dir="$repo_root/artifacts"
package_output="$repo_root/src/GenderNameEstimator.UI.Mac/bin/Release/net10.0-macos/GnE-$version.pkg"
dotnet_command="${DOTNET_COMMAND:-/usr/local/share/dotnet/dotnet}"

if [[ ! -x "$dotnet_command" ]]; then
  print -u2 "Release packaging requires the official Microsoft .NET SDK. Set DOTNET_COMMAND to its dotnet executable."
  exit 1
fi

function validate_native_dependencies() {
  local target_app="$1"
  local binary dependency
  local native_binaries=("$target_app/Contents/MacOS/GnE" "$target_app"/Contents/MonoBundle/*.dylib(N))

  for binary in "${native_binaries[@]}"; do
    while IFS= read -r dependency; do
      case "$dependency" in
        @*|/System/*|/usr/lib/*) ;;
        *)
          print -u2 "Non-relocatable dependency in $binary: $dependency"
          return 1
          ;;
      esac
    done < <(otool -L "$binary" | sed -n '2,$s/^[[:space:]]*\([^[:space:]]*\).*/\1/p')
  done
}

function smoke_test_app() {
  local target_app="$1"
  local smoke_log smoke_pid smoke_status
  smoke_log="$(mktemp)"
  "$target_app/Contents/MacOS/GnE" >"$smoke_log" 2>&1 &
  smoke_pid=$!
  sleep 3
  if ! kill -0 "$smoke_pid" 2>/dev/null; then
    smoke_status=0
    wait "$smoke_pid" || smoke_status=$?
    print -u2 "GnE launch smoke test failed with status $smoke_status."
    tail -40 "$smoke_log" >&2
    rm -f "$smoke_log"
    return 1
  fi
  kill "$smoke_pid"
  wait "$smoke_pid" 2>/dev/null || true
  rm -f "$smoke_log"
}

if [[ ! "$version" =~ '^[0-9]+\.[0-9]+\.[0-9]+$' ]]; then
  print -u2 "VERSION must contain a semantic version."
  exit 1
fi

rm -rf "$artifacts_dir"
mkdir -p "$artifacts_dir"
rm -f "$package_output"

"$repo_root/scripts/inject_secrets.zsh"

"$dotnet_command" test "$solution" --configuration Release
"$dotnet_command" clean "$project" --configuration Release
"$dotnet_command" clean "$project" --configuration Release --runtime osx-arm64

"$dotnet_command" build "$project" \
  --configuration Release \
  -p:CreatePackage=true \
  -p:CodesignKey="$application_identity" \
  -p:PackageSigningKey="$installer_identity"

app="$repo_root/src/GenderNameEstimator.UI.Mac/bin/Release/net10.0-macos/GnE.app"
if [[ ! -d "$app" ]]; then
  print -u2 "Expected GnE.app build output was not found."
  exit 1
fi

info_plist="$app/Contents/Info.plist"
display_version="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$info_plist")"
build_version="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleVersion' "$info_plist")"
if [[ "$display_version" != "$version" || "$build_version" != "$version" ]]; then
  print -u2 "App version mismatch: expected $version, found $display_version ($build_version)."
  exit 1
fi

if [[ "$(lipo -archs "$app/Contents/MacOS/GnE")" != "arm64" ]]; then
  print -u2 "GnE.app must contain only the arm64 architecture."
  exit 1
fi

codesign --verify --deep --strict --verbose=2 "$app"
validate_native_dependencies "$app"
smoke_test_app "$app"

packages=("$package_output"(N))
if (( ${#packages} != 1 )); then
  print -u2 "Expected exactly one GnE-$version.pkg build output; found ${#packages}."
  exit 1
fi

package="${packages[1]}"
pkgutil --check-signature "$package"

inspection_dir="$(mktemp -d)"
trap 'rm -rf "$inspection_dir"' EXIT
pkgutil --expand-full "$package" "$inspection_dir/expanded"
packaged_apps=("$inspection_dir"/expanded/**/GnE.app(N))
if (( ${#packaged_apps} != 1 )); then
  print -u2 "Expected exactly one GnE.app in the installer; found ${#packaged_apps}."
  exit 1
fi
codesign --verify --deep --strict --verbose=2 "${packaged_apps[1]}"
validate_native_dependencies "${packaged_apps[1]}"
smoke_test_app "${packaged_apps[1]}"

xcrun notarytool submit "$package" --keychain-profile "$notary_profile" --wait
xcrun stapler staple "$package"
xcrun stapler validate "$package"
spctl --assess --type install --verbose=2 "$package"

/bin/cp "$package" "$artifacts_dir/GnE-$version.pkg"
shasum -a 256 "$artifacts_dir/GnE-$version.pkg"
