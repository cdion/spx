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

vm         := env("VM", "core@mostlyhuman.ca")
web_image  := "localhost/spx-web:latest"
silo_image := "localhost/spx-silo:latest"

# List available recipes
default:
    @just --list

# ===========================================================================
# Deploy
# ===========================================================================

# Full deploy: build images, transfer to VM, run migrations, restart services
deploy: build push migrate restart-app
    @echo "==> Done. https://mostlyhuman.ca"

# Build both container images locally
build:
    @echo "==> Building spx-web..."
    podman build -f deploy/Containerfile --target web  -t {{web_image}}  .
    @echo "==> Building spx-silo..."
    podman build -f deploy/Containerfile --target silo -t {{silo_image}} .

# Stream both images to the VM (no registry needed)
push:
    @echo "==> Transferring images to {{vm}}..."
    podman save {{web_image}} {{silo_image}} | gzip | ssh {{vm}} "sudo podman load"

# Run pending EF Core migrations against appdb using the bundled efbundle executable.
# The bundle connects directly to Postgres — it does not start Orleans or the web host.
migrate:
    @echo "==> Running EF migrations on {{vm}}..."
    ssh {{vm}} "sudo podman run --rm \
        --network systemd-spx \
        --env-file /etc/spx/prod.env \
        --entrypoint /app/migrate \
        {{web_image}}"

# Restart the application services (silo first, then web, then caddy)
restart-app:
    @echo "==> Restarting services on {{vm}}..."
    ssh {{vm}} "sudo systemctl restart spx-silo spx-web caddy"

# ===========================================================================
# First-time bootstrap
# ===========================================================================

# Install Quadlet and config files onto the VM, then enable all services
bootstrap: install-files deploy-secrets daemon-reload _enable-services
    @echo "==> Bootstrap complete. Run 'just deploy' to ship the first images."

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
        sudo podman rm -f postgres caddy nex 2>/dev/null; \
        echo done"

# Follow logs for a service (e.g.: just logs spx-web)
logs service:
    ssh {{vm}} "sudo journalctl -u {{service}} -f"

# Show status of all Spx services
status:
    ssh {{vm}} "sudo systemctl status postgres redis spx-silo spx-web caddy --no-pager -l"

# Show running containers on the VM
ps:
    ssh {{vm}} "sudo podman ps"
