#!/usr/bin/env fish

set script_dir (cd (dirname (status filename)); and pwd)
set workspace_root (cd $script_dir/../..; and pwd)
set apphost_project $workspace_root/src/Spx.AppHost/Spx.AppHost.csproj

# Stop any previous instance by killing its systemd scope cgroup atomically.
# This handles the case where VS Code sent SIGKILL (e.g. Reload Window), which
# bypasses all signal handlers. The cgroup survives the death of any userspace
# process, so systemd can always clean it up by name.
systemctl --user stop spx-apphost.scope 2>/dev/null

# Run dotnet watch inside a transient user scope so the entire process tree
# (dotnet watch → AppHost → DCP → silo/web) is tracked in one cgroup.
# exec replaces the fish process; no signal handler or setsid needed.
#
# dotnet watch on the AppHost is the canonical Aspire dev loop: DCP detects
# file changes in any referenced project and restarts only the affected service.
# DOTNET_USE_POLLING_FILE_WATCHER avoids inotify descriptor limits on Linux.
set -x DOTNET_USE_POLLING_FILE_WATCHER 1
exec systemd-run --user --scope --unit=spx-apphost \
    dotnet watch --project $apphost_project run
