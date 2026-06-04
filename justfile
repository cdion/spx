import './ci.just'
import './deploy.just'
import './infra.just'
import './dev.just'

# Spx ops
# Usage: just <recipe>
#
# Production deploy requires:
#   - just      (https://github.com/casey/just)
#   - podman
#   - ssh access to the VM as core@<VM>
#
# Development (no VM needed):
#   just dev-run                # AppHost (Aspire dashboard)
#   just dev-watch              # AppHost with dotnet watch
#   just dev-tailwind-watch     # Tailwind CSS watch
#   just dev-watch-playground   # Playground with dotnet watch
#   just test-project tests/Spx.Nexus.Domain.Tests
#   just build-project src/Spx.Web
#
# Set the VM address either in the environment or by overriding the variable:
#   VM=core@1.2.3.4 just deploy

set shell := ["bash", "-euo", "pipefail", "-c"]

dotnet        := env("DOTNET", "dotnet")
container_cli := env("CONTAINER_CLI", "podman")
vm            := env("VM", "core@mostlyhuman.ca")
registry      := env("IMAGE_REGISTRY", "ghcr.io")
namespace     := env("IMAGE_NAMESPACE", "cdion")
git_sha       := `git rev-parse --short=12 HEAD 2>/dev/null || echo dev`
git_ref       := `git rev-parse HEAD 2>/dev/null || echo dev`
app_version   := env("SPX_VERSION", "0.0.0-sha." + git_sha)
image_tag     := env("IMAGE_TAG", app_version)
deploy_tag    := env("DEPLOY_TAG", "prod")
image_source  := env("IMAGE_SOURCE", "https://github.com/cdion/spx")
web_image     := registry + "/" + namespace + "/spx-web:" + image_tag
silo_image    := registry + "/" + namespace + "/spx-silo:" + image_tag
deploy_web_image  := registry + "/" + namespace + "/spx-web:" + deploy_tag
deploy_silo_image := registry + "/" + namespace + "/spx-silo:" + deploy_tag

# List available recipes
default:
    @just --list

# Print the canonical build version
version:
    @echo {{app_version}}
