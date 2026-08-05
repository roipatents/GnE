#!/bin/zsh
set -euo pipefail

repo_root="${0:A:h:h}"
template="$repo_root/appsettings.Secrets.template.json"
output="$repo_root/appsettings.Secrets.json"
stamp="$output.roi-op-injected.stamp"
lock="$output.roi-op-injected.lock"
today="$(date +%Y%m%d)"
template_hash="$(shasum -a 256 "$template" | awk '{print $1}')"
expected_stamp="$today|$template_hash"

while ! mkdir "$lock" 2>/dev/null; do
  sleep 0.1
done
trap 'rmdir "$lock" 2>/dev/null || true' EXIT

if [[ -f "$output" && -f "$stamp" && "$(<"$stamp")" == "$expected_stamp" ]]; then
  exit 0
fi

op inject --force --in-file "$template" --out-file "$output"
print -r -- "$expected_stamp" > "$stamp"
