#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../.." && pwd)"
tailwind_version="${TAILWIND_VERSION:-v4.3.0}"
tailwind_cli="$repo_root/tools/tailwind/bin/tailwindcss-linux-x64"
tailwind_url="https://github.com/tailwindlabs/tailwindcss/releases/download/${tailwind_version}/tailwindcss-linux-x64"

if [ -x "$tailwind_cli" ]; then
    echo "==> Tailwind CLI already present at $tailwind_cli."
    exit 0
fi

echo "==> Downloading Tailwind CLI ${tailwind_version}..."
mkdir -p "$(dirname -- "$tailwind_cli")"
curl -fsSL -o "$tailwind_cli" "$tailwind_url"
chmod +x "$tailwind_cli"
