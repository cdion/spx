#!/bin/sh
# Installs the pre-commit hook from tools/dev/pre-commit into .git/hooks/.
# Run once after cloning: sh tools/dev/install-hooks.sh

set -e

REPO_ROOT="$(git rev-parse --show-toplevel)"
HOOK_SRC="$REPO_ROOT/tools/dev/pre-commit"
HOOK_DST="$REPO_ROOT/.git/hooks/pre-commit"

cp "$HOOK_SRC" "$HOOK_DST"
chmod +x "$HOOK_DST"

echo "✓ pre-commit hook installed at $HOOK_DST"
