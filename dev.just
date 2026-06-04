# Development — build, test, coverage, tailwind, run, watch

# Build a specific project (e.g.: just dev-build-project src/Spx.Web)
dev-build-project project:
    dotnet build {{project}}

# Run tests in a specific project (e.g.: just dev-test-project tests/Spx.Nexus.Domain.Tests)
dev-test-project project:
    dotnet test {{project}}

# Run all tests
dev-test-all:
    dotnet test Spx.slnx

# Collect XPlat Code Coverage for a single project
dev-coverage project:
    dotnet test {{project}} --collect:"XPlat Code Coverage" --results-directory coverage/testresults

# Clean coverage data, collect for all projects, and generate HTML report
dev-coverage-full:
    rm -rf coverage
    dotnet test tests/Spx.Account.Application.Tests --collect:"XPlat Code Coverage" --results-directory coverage/testresults
    dotnet test tests/Spx.Nexus.Domain.Tests --collect:"XPlat Code Coverage" --results-directory coverage/testresults
    dotnet test tests/Spx.Nexus.Simulation.Tests --collect:"XPlat Code Coverage" --results-directory coverage/testresults
    dotnet test tests/Spx.Game.Application.Tests --collect:"XPlat Code Coverage" --results-directory coverage/testresults
    dotnet test tests/Spx.Game.Application.IntegrationTests --collect:"XPlat Code Coverage" --results-directory coverage/testresults
    dotnet test tests/Spx.Grains.Tests --collect:"XPlat Code Coverage" --results-directory coverage/testresults
    dotnet test tests/Spx.Grains.IntegrationTests --collect:"XPlat Code Coverage" --results-directory coverage/testresults
    dotnet test tests/Spx.Web.Tests --collect:"XPlat Code Coverage" --results-directory coverage/testresults
    mkdir -p coverage/html
    dotnet tool run reportgenerator \
        -reports:coverage/testresults/**/coverage.cobertura.xml \
        -targetdir:coverage/html \
        -reporttypes:Html
    @echo "==> Coverage report at coverage/html/index.html"

# Build the Tailwind CSS bundle once
dev-tailwind:
    ./tools/tailwind/bin/tailwindcss-linux-x64 \
        -i src/Spx.Web.Components/Styles/app.css \
        -o src/Spx.Web.Components/wwwroot/app.css \
        --minify

# Watch Tailwind source files and rebuild on change (background)
dev-tailwind-watch:
    ./tools/tailwind/bin/tailwindcss-linux-x64 \
        -i src/Spx.Web.Components/Styles/app.css \
        -o src/Spx.Web.Components/wwwroot/app.css \
        --watch

# Start the AppHost (Aspire dashboard)
dev-run:
    dotnet run --project src/Spx.AppHost/Spx.AppHost.csproj

# Watch the AppHost with dotnet watch (run in a separate terminal)
dev-watch:
    # Kill previous scope so systemd cleans up the entire cgroup
    systemctl --user stop spx-apphost 2>/dev/null || true
    systemd-run --user --unit=spx-apphost --scope \
        -p Delegate=yes \
        -- \
        env DOTNET_USE_POLLING_FILE_WATCHER=1 \
            dotnet watch --project src/Spx.AppHost/Spx.AppHost.csproj run

# Watch the Playground with dotnet watch (run in a separate terminal)
dev-watch-playground:
    # Kill previous scope so systemd cleans up the entire cgroup
    systemctl --user stop spx-playground 2>/dev/null || true
    systemd-run --user --unit=spx-playground --scope \
        -p Delegate=yes \
        -- \
        env DOTNET_USE_POLLING_FILE_WATCHER=1 \
            dotnet watch --project src/Spx.Web.Playground/Spx.Web.Playground.csproj

# Run EF Core migrations against the local database
# Requires a Postgres container to be running (from `just dev-watch` or `just dev-run`)
dev-db-migrate:
    container=$(podman ps --filter name=postgres --noheading 2>/dev/null | head -1 | awk '{print $NF}'); \
    if [ -z "$container" ]; then \
        echo "No Postgres container found. Start the AppHost first: just dev-watch" >&2; \
        exit 1; \
    fi; \
    host_port=$(podman port "$container" 5432 2>/dev/null | head -1 | sed 's/.*://'); \
    password=$(podman exec "$container" env 2>/dev/null | sed -n 's/^POSTGRES_PASSWORD=//p'); \
    export APPDB_URI="Host=localhost;Port=$host_port;Database=appdb;Username=postgres;Password=$password"; \
    echo "Migrating appdb on localhost:$host_port..."; \
    dotnet ef database update --project src/Spx.Data
