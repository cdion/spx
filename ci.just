# CI pipeline — build, restore, test

# Ensure the repo-local Tailwind standalone binary exists for web builds
ci-ensure-tailwind:
    @./tools/tailwind/install.sh

# Restore .NET workloads and local tools
ci-restore: ci-ensure-tailwind
    @echo "==> Restoring tools and packages..."
    {{dotnet}} tool restore
    {{dotnet}} restore Spx.slnx

# Build the solution with the canonical app version embedded in assemblies
ci-build:
    @echo "==> Building solution (version {{app_version}})..."
    {{dotnet}} build Spx.slnx -p:Version={{app_version}} -p:InformationalVersion={{app_version}}

# Run the full test suite without rebuilding
ci-test:
    {{dotnet}} test Spx.slnx --no-build

# Canonical local CI entry point: restore → build → test
ci: ci-restore ci-build ci-test
    @echo "==> CI checks completed."
