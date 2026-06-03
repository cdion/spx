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

# Watch the AppHost with hot reload (background, restarts on edit)
dev-watch:
    @systemctl --user stop spx-apphost.scope 2>/dev/null; true
    env DOTNET_USE_POLLING_FILE_WATCHER=1 \
        DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1 \
        systemd-run --user --scope --unit=spx-apphost \
        dotnet watch --project src/Spx.AppHost/Spx.AppHost.csproj run

# Watch the Playground with hot reload (background)
dev-watch-playground:
    env DOTNET_USE_POLLING_FILE_WATCHER=1 \
        DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1 \
        dotnet watch --project src/Spx.Web.Playground/Spx.Web.Playground.csproj

# Run both Playground and Tailwind watch in parallel
dev-playground:
    shopt -s huponexit; \
    trap 'kill 0 2>/dev/null; exit' INT TERM HUP; \
    just dev-tailwind-watch & \
    env DOTNET_USE_POLLING_FILE_WATCHER=1 \
        DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1 \
        dotnet watch --project src/Spx.Web.Playground/Spx.Web.Playground.csproj & \
    wait

# Run both AppHost and Tailwind watch in parallel
dev:
    shopt -s huponexit; \
    trap 'kill 0 2>/dev/null; exit' INT TERM HUP; \
    just dev-tailwind-watch & \
    env DOTNET_USE_POLLING_FILE_WATCHER=1 \
        DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1 \
        dotnet watch --project src/Spx.AppHost/Spx.AppHost.csproj run & \
    wait
