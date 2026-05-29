# Spx production ops
# Usage: just <recipe>
#
# Prerequisites on your workstation:
#   - just      (https://github.com/casey/just)
#   - podman
#   - ssh access to the VM as core@<VM>
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

# ==========================================================================
# Local validation
# ==========================================================================

# Restore .NET workloads and local tools
restore:
    @echo "==> Restoring tools and packages..."
    {{dotnet}} tool restore
    {{dotnet}} restore Spx.slnx

# Build the solution with the canonical app version embedded in assemblies
build:
    @echo "==> Building solution (version {{app_version}})..."
    {{dotnet}} build Spx.slnx -p:Version={{app_version}} -p:InformationalVersion={{app_version}}

# Run the full test suite without rebuilding
test:
    @echo "==> Running account application tests..."
    {{dotnet}} test tests/Spx.Account.Application.Tests/Spx.Account.Application.Tests.csproj --no-build
    @echo "==> Running game application tests..."
    {{dotnet}} test tests/Spx.Game.Application.Tests/Spx.Game.Application.Tests.csproj --no-build
    @echo "==> Running game application integration tests..."
    {{dotnet}} test tests/Spx.Game.Application.IntegrationTests/Spx.Game.Application.IntegrationTests.csproj --no-build
    @echo "==> Running grains tests..."
    {{dotnet}} test tests/Spx.Grains.Tests/Spx.Grains.Tests.csproj --no-build
    @echo "==> Running grains integration tests..."
    {{dotnet}} test tests/Spx.Grains.IntegrationTests/Spx.Grains.IntegrationTests.csproj --no-build
    @echo "==> Running nexus domain tests..."
    {{dotnet}} test tests/Spx.Nexus.Domain.Tests/Spx.Nexus.Domain.Tests.csproj --no-build
    @echo "==> Running web tests..."
    {{dotnet}} test tests/Spx.Web.Tests/Spx.Web.Tests.csproj --no-build

# Canonical local CI entry point
ci: restore build test
    @echo "==> CI checks completed."

# ===========================================================================
# Images and deploy
# ===========================================================================

# Build both production container images locally
image-build:
    @echo "==> Building spx-web image {{web_image}}..."
    {{container_cli}} build -f deploy/Containerfile \
        --target web \
        --build-arg SPX_VERSION={{app_version}} \
        --build-arg SPX_SOURCE={{image_source}} \
        --build-arg SPX_REVISION={{git_ref}} \
        -t {{web_image}} \
        .
    @echo "==> Building spx-silo image {{silo_image}}..."
    {{container_cli}} build -f deploy/Containerfile \
        --target silo \
        --build-arg SPX_VERSION={{app_version}} \
        --build-arg SPX_SOURCE={{image_source}} \
        --build-arg SPX_REVISION={{git_ref}} \
        -t {{silo_image}} \
        .

# Push the locally built images to the configured registry
image-push:
    @echo "==> Pushing {{web_image}}..."
    {{container_cli}} push {{web_image}}
    @echo "==> Pushing {{silo_image}}..."
    {{container_cli}} push {{silo_image}}

# Build and publish the versioned images to the configured registry
publish: image-build image-push
    @echo "==> Published {{web_image}} and {{silo_image}}."

# Promote the current build to the stable production tags
promote-prod:
    @echo "==> Promoting {{web_image}} to {{deploy_web_image}}..."
    {{container_cli}} tag {{web_image}} {{deploy_web_image}}
    {{container_cli}} push {{deploy_web_image}}
    @echo "==> Promoting {{silo_image}} to {{deploy_silo_image}}..."
    {{container_cli}} tag {{silo_image}} {{deploy_silo_image}}
    {{container_cli}} push {{deploy_silo_image}}

# Publish the current build and promote it to the stable deployment tags
release-prod: publish promote-prod
    @echo "==> Promoted {{app_version}} to the {{deploy_tag}} deployment tags."

# Pull the selected images onto the VM before running migrations or restarting services
pull-images:
    @echo "==> Pulling images on {{vm}}..."
    ssh {{vm}} "sudo {{container_cli}} pull {{deploy_web_image}} && sudo {{container_cli}} pull {{deploy_silo_image}}"

# Run pending EF Core migrations against appdb using the bundled efbundle executable.
# The bundle connects directly to Postgres — it does not start Orleans or the web host.
migrate:
    @echo "==> Running EF migrations on {{vm}}..."
    ssh {{vm}} "sudo {{container_cli}} run --rm \
        --network systemd-spx \
        --env-file /etc/spx/prod.env \
        --entrypoint /app/migrate \
        {{deploy_web_image}}"

# Restart the application services (silo first, then web, then caddy)
restart-app:
    @echo "==> Restarting services on {{vm}}..."
    ssh {{vm}} "sudo systemctl restart spx-silo spx-web caddy"

# Full deploy: pull images on the VM, run migrations, restart services
deploy: pull-images migrate restart-app
    @echo "==> Done. https://mostlyhuman.ca"

# ===========================================================================
# First-time bootstrap
# ===========================================================================

# Install Quadlet and config files onto the VM, then enable all services
bootstrap: login-registry install-files deploy-secrets daemon-reload _enable-services
    @echo "==> Bootstrap complete. Run 'just deploy' to ship the first images."

# Authenticate the VM's root Podman store to GHCR when private images are used.
login-registry:
    @if [ -n "${GHCR_USERNAME:-}" ] && [ -n "${GHCR_TOKEN:-}" ]; then \
        echo "==> Logging {{vm}} into {{registry}}..."; \
        printf "%s" "$GHCR_TOKEN" | ssh {{vm}} "sudo {{container_cli}} login {{registry}} --username $GHCR_USERNAME --password-stdin"; \
    else \
        echo "==> Skipping registry login; set GHCR_USERNAME and GHCR_TOKEN for private images."; \
    fi

# Copy Quadlet units, Caddyfile, and DB init script to the VM
install-files:
    @echo "==> Installing files to {{vm}}..."
    ssh {{vm}} "sudo install -d -m 0755 /etc/spx /etc/spx/initdb /etc/containers/systemd"
    scp deploy/Caddyfile                           {{vm}}:/tmp/_Caddyfile
    scp deploy/initdb/00-databases.sql             {{vm}}:/tmp/_00-databases.sql
    scp deploy/quadlet/spx.network                 {{vm}}:/tmp/_spx.network
    scp deploy/quadlet/postgres.container          {{vm}}:/tmp/_postgres.container
    scp deploy/quadlet/redis.container             {{vm}}:/tmp/_redis.container
    scp deploy/quadlet/spx-silo.container          {{vm}}:/tmp/_spx-silo.container
    scp deploy/quadlet/spx-web.container           {{vm}}:/tmp/_spx-web.container
    scp deploy/quadlet/caddy.container             {{vm}}:/tmp/_caddy.container
    ssh {{vm}} " \
        sudo install -m 0644 /tmp/_Caddyfile              /etc/spx/Caddyfile && \
        sudo install -m 0644 /tmp/_00-databases.sql       /etc/spx/initdb/00-databases.sql && \
        sudo install -m 0644 /tmp/_spx.network            /etc/containers/systemd/spx.network && \
        sudo install -m 0644 /tmp/_postgres.container     /etc/containers/systemd/postgres.container && \
        sudo install -m 0644 /tmp/_redis.container        /etc/containers/systemd/redis.container && \
        sudo install -m 0644 /tmp/_spx-silo.container     /etc/containers/systemd/spx-silo.container && \
        sudo install -m 0644 /tmp/_spx-web.container      /etc/containers/systemd/spx-web.container && \
        sudo install -m 0644 /tmp/_caddy.container        /etc/containers/systemd/caddy.container && \
        rm -f /tmp/_Caddyfile /tmp/_00-databases.sql \
              /tmp/_spx.network /tmp/_postgres.container /tmp/_redis.container \
              /tmp/_spx-silo.container /tmp/_spx-web.container /tmp/_caddy.container"
    @echo "==> Files installed."

# Upload .env.prod to /etc/spx/prod.env on the VM (mode 0600)
deploy-secrets:
    @echo "==> Uploading secrets to {{vm}}..."
    scp .env.prod {{vm}}:/tmp/_prod.env
    ssh {{vm}} "sudo install -m 0600 -o root -g root /tmp/_prod.env /etc/spx/prod.env && rm -f /tmp/_prod.env"
    @echo "==> Secrets installed."

# Reload systemd so Quadlet generates unit files from the .container definitions
daemon-reload:
    ssh {{vm}} "sudo systemctl daemon-reload"

_enable-services:
    # Quadlet-generated units cannot be `enable`d — daemon-reload already wired
    # them into multi-user.target via the [Install] WantedBy= in each .container file.
    ssh {{vm}} "sudo systemctl start postgres redis spx-silo spx-web caddy"

# ===========================================================================
# Diagnostics
# ===========================================================================

# Stop and remove any old nex containers that conflict with spx names/ports
retire-nex:
    @echo "==> Stopping old nex containers on {{vm}}..."
    ssh {{vm}} "sudo systemctl stop caddy postgres nex 2>/dev/null; \
        sudo {{container_cli}} rm -f postgres caddy nex 2>/dev/null; \
        echo done"

# Follow logs for a service (e.g.: just logs spx-web)
logs service:
    ssh {{vm}} "sudo journalctl -u {{service}} -f"

# Show status of all Spx services
status:
    ssh {{vm}} "sudo systemctl status postgres redis spx-silo spx-web caddy --no-pager -l"

# Show running containers on the VM
ps:
    ssh {{vm}} "sudo {{container_cli}} ps"
