# Infrastructure — bootstrap, database reset, diagnostics

# ── Database ───────────────────────────────────────────────────────────────────

# Clear local app data from the AppHost-managed appdb (preserves EF migration history)
infra-reset-appdb:
    @container="$({{container_cli}} ps --quiet --filter name=postgres- | head -n 1)"; \
    if [ -z "$container" ]; then \
        echo "==> No running local Postgres container found. Start the AppHost first."; \
        exit 1; \
    fi; \
    username="$({{container_cli}} exec "$container" printenv POSTGRES_USER)"; \
    password="$({{container_cli}} exec "$container" printenv POSTGRES_PASSWORD)"; \
    if [ -z "$username" ] || [ -z "$password" ]; then \
        echo "==> Could not read POSTGRES_USER/POSTGRES_PASSWORD from $container."; \
        exit 1; \
    fi; \
    echo "==> Clearing application tables from appdb..."; \
    {{container_cli}} exec -e PGPASSWORD="$password" "$container" \
        psql -h localhost -U "$username" -d appdb -c "DO \$\$ DECLARE truncate_sql text; BEGIN SELECT 'TRUNCATE TABLE ' || string_agg(format('%I.%I', schemaname, tablename), ', ') || ' RESTART IDENTITY CASCADE;' INTO truncate_sql FROM pg_tables WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory'; IF truncate_sql IS NOT NULL THEN EXECUTE truncate_sql; END IF; END \$\$;" && \
    echo "==> appdb cleared. Restart the AppHost."

# Clear local Orleans persistent grain state from the AppHost-managed orleansdb
infra-reset-orleansdb:
    @container="$({{container_cli}} ps --quiet --filter name=postgres- | head -n 1)"; \
    if [ -z "$container" ]; then \
        echo "==> No running local Postgres container found. Start the AppHost first."; \
        exit 1; \
    fi; \
    username="$({{container_cli}} exec "$container" printenv POSTGRES_USER)"; \
    password="$({{container_cli}} exec "$container" printenv POSTGRES_PASSWORD)"; \
    if [ -z "$username" ] || [ -z "$password" ]; then \
        echo "==> Could not read POSTGRES_USER/POSTGRES_PASSWORD from $container."; \
        exit 1; \
    fi; \
    echo "==> Clearing Orleans grain storage rows..."; \
    {{container_cli}} exec -e PGPASSWORD="$password" "$container" \
        psql -h localhost -U "$username" -d orleansdb -c "TRUNCATE TABLE orleansstorage;" && \
    echo "==> Storage cleared. Restart the AppHost to drop in-memory activations."

# ── Bootstrap ──────────────────────────────────────────────────────────────────

# Install Quadlet and config files onto the VM, then enable all services
infra-bootstrap: infra-login-registry infra-install-files infra-deploy-secrets infra-daemon-reload infra-enable-services
    @echo "==> Bootstrap complete. Run 'just deploy' to ship the first images."

# Authenticate the VM's root Podman store to GHCR when private images are used
infra-login-registry:
    @if [ -n "${GHCR_USERNAME:-}" ] && [ -n "${GHCR_TOKEN:-}" ]; then \
        echo "==> Logging {{vm}} into {{registry}}..."; \
        printf "%s" "$GHCR_TOKEN" | ssh {{vm}} "sudo {{container_cli}} login {{registry}} --username $GHCR_USERNAME --password-stdin"; \
    else \
        echo "==> Skipping registry login; set GHCR_USERNAME and GHCR_TOKEN for private images."; \
    fi

# Copy Quadlet units, Caddyfile, and DB init script to the VM
infra-install-files:
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
infra-deploy-secrets:
    @echo "==> Uploading secrets to {{vm}}..."
    scp .env.prod {{vm}}:/tmp/_prod.env
    ssh {{vm}} "sudo install -m 0600 -o root -g root /tmp/_prod.env /etc/spx/prod.env && rm -f /tmp/_prod.env"
    @echo "==> Secrets installed."

# Reload systemd so Quadlet generates unit files from the .container definitions
infra-daemon-reload:
    ssh {{vm}} "sudo systemctl daemon-reload"

# Start all services on the VM after bootstrap
infra-enable-services:
    ssh {{vm}} "sudo systemctl start postgres redis spx-silo spx-web caddy"

# ── Diagnostics ────────────────────────────────────────────────────────────────

# Follow logs for a service (e.g.: just infra-logs spx-web)
infra-logs service:
    ssh {{vm}} "sudo journalctl -u {{service}} -f"

# Show status of all Spx services
infra-status:
    ssh {{vm}} "sudo systemctl status postgres redis spx-silo spx-web caddy --no-pager -l"

# Show running containers on the VM
infra-ps:
    ssh {{vm}} "sudo {{container_cli}} ps"
