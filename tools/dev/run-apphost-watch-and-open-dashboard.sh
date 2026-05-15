#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
workspace_root="$(cd "$script_dir/../.." && pwd)"
apphost_project="$workspace_root/src/Spx.AppHost/Spx.AppHost.csproj"

fifo_path="$(mktemp -u)"
mkfifo "$fifo_path"

child_pid=""

cleanup() {
    local status=$?

    if [[ -n "$child_pid" ]]; then
        kill "$child_pid" 2>/dev/null || true
        wait "$child_pid" 2>/dev/null || true
    fi

    rm -f "$fifo_path"
    exit "$status"
}

trap cleanup INT TERM EXIT

(
    cd "$workspace_root"
    DOTNET_USE_POLLING_FILE_WATCHER=1 dotnet watch --project "$apphost_project" run >"$fifo_path" 2>&1
) &
child_pid=$!

opened_dashboard=0

while IFS= read -r line; do
    printf '%s\n' "$line"

    if [[ $opened_dashboard -eq 0 && "$line" =~ Login\ to\ the\ dashboard\ at\ (https?://[^[:space:]]+) ]]; then
        dashboard_url="${BASH_REMATCH[1]}"
        opened_dashboard=1

        if command -v code >/dev/null 2>&1; then
            code --open-url "$dashboard_url" >/dev/null 2>&1 &
        elif command -v xdg-open >/dev/null 2>&1; then
            xdg-open "$dashboard_url" >/dev/null 2>&1 &
        else
            printf 'Dashboard URL: %s\n' "$dashboard_url" >&2
        fi
    fi
done <"$fifo_path"

wait "$child_pid"
child_status=$?

trap - INT TERM EXIT
rm -f "$fifo_path"
exit "$child_status"