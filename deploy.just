# Deploy — container images, migration, service restart

# Build both production container images locally
deploy-build-images:
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
deploy-push-images:
    @echo "==> Pushing {{web_image}}..."
    {{container_cli}} push {{web_image}}
    @echo "==> Pushing {{silo_image}}..."
    {{container_cli}} push {{silo_image}}

# Build and publish the versioned images to the configured registry
deploy-publish: deploy-build-images deploy-push-images
    @echo "==> Published {{web_image}} and {{silo_image}}."

# Promote the current build to the stable production tags
deploy-promote-prod:
    @echo "==> Promoting {{web_image}} to {{deploy_web_image}}..."
    @{{container_cli}} image exists {{web_image}} || {{container_cli}} pull {{web_image}}
    {{container_cli}} tag {{web_image}} {{deploy_web_image}}
    {{container_cli}} push {{deploy_web_image}}
    @echo "==> Promoting {{silo_image}} to {{deploy_silo_image}}..."
    @{{container_cli}} image exists {{silo_image}} || {{container_cli}} pull {{silo_image}}
    {{container_cli}} tag {{silo_image}} {{deploy_silo_image}}
    {{container_cli}} push {{deploy_silo_image}}

# Publish the current build and promote it to the stable deployment tags
deploy-release-prod: deploy-publish deploy-promote-prod
    @echo "==> Promoted {{app_version}} to the {{deploy_tag}} deployment tags."

# Pull the selected images onto the VM before running migrations or restarting services
deploy-pull-images:
    @echo "==> Pulling images on {{vm}}..."
    ssh {{vm}} "sudo {{container_cli}} pull {{deploy_web_image}} && sudo {{container_cli}} pull {{deploy_silo_image}}"

# Run pending EF Core migrations against appdb using the bundled efbundle executable
deploy-migrate:
    @echo "==> Running EF migrations on {{vm}}..."
    ssh {{vm}} "sudo {{container_cli}} run --rm \
        --network systemd-spx \
        --env-file /etc/spx/prod.env \
        --entrypoint /app/migrate \
        {{deploy_web_image}}"

# Restart the application services (silo first, then web, then caddy)
deploy-restart-app:
    @echo "==> Restarting services on {{vm}}..."
    ssh {{vm}} "sudo systemctl restart spx-silo spx-web caddy"

# Full deploy: pull images on the VM, run migrations, restart services
deploy: deploy-pull-images deploy-migrate deploy-restart-app
    @echo "==> Done. https://mostlyhuman.ca"
