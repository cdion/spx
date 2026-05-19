#!/usr/bin/env fish

set script_dir (cd (dirname (status filename)); and pwd)
set workspace_root (cd $script_dir/../..; and pwd)
set apphost_project $workspace_root/src/Spx.AppHost/Spx.AppHost.csproj

# Kill any orphaned processes left over from a previous run before starting fresh.
# This handles the case where VS Code killed the task with SIGKILL, which bypasses
# signal handlers and leaves dotnet watch, DCP, silo, and web as orphans.
pkill -TERM -f "Spx.AppHost" 2>/dev/null
pkill -TERM -f "Spx.Silo"    2>/dev/null
pkill -TERM -f "Spx.Web"     2>/dev/null
pkill -TERM -f "tools/dcp"   2>/dev/null
sleep 1

set -g child_pid ""

# --on-signal handles SIGTERM (normal task stop), SIGINT (Ctrl+C), and SIGHUP
# (terminal close). SIGKILL cannot be caught; the startup sweep above handles
# that case on the next run.
function _cleanup --on-signal INT --on-signal TERM --on-signal HUP
    if test -n "$child_pid"
        # Kill the entire process group: dotnet watch → AppHost → DCP → silo/web
        kill -TERM -- -$child_pid 2>/dev/null
        # Give the AppHost time to tell DCP to stop managed resources gracefully.
        sleep 3
        kill -KILL -- -$child_pid 2>/dev/null
        wait $child_pid 2>/dev/null
    end
    exit 0
end

# setsid runs dotnet watch in a new process group so kill -- -$child_pid
# reaches the full tree. $last_pid gives the process group leader PID.
#
# dotnet watch on the AppHost is the canonical Aspire dev loop: DCP detects
# file changes in any referenced project and restarts only the affected
# service, enabling per-service restart on code changes.
# DOTNET_USE_POLLING_FILE_WATCHER avoids inotify descriptor limits on Linux.
set -x DOTNET_USE_POLLING_FILE_WATCHER 1
setsid dotnet watch --project $apphost_project run &
set child_pid $last_pid

wait $child_pid
