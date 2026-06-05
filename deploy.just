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

# Build, promote, and send the versioned images to the VM
deploy-publish: deploy-build-images deploy-promote-prod deploy-pull-images
    @echo "==> Published {{app_version}} to {{vm}}."

# Promote the current build to the stable production tags
deploy-promote-prod:
    @echo "==> Promoting {{web_image}} to {{deploy_web_image}}..."
    @{{container_cli}} image exists {{web_image}} || {{container_cli}} pull {{web_image}}
    {{container_cli}} tag {{web_image}} {{deploy_web_image}}
    @echo "==> Promoting {{silo_image}} to {{deploy_silo_image}}..."
    @{{container_cli}} image exists {{silo_image}} || {{container_cli}} pull {{silo_image}}
    {{container_cli}} tag {{silo_image}} {{deploy_silo_image}}

# Publish and deliver the current build to the VM as the stable deployment tags
deploy-release-prod: deploy-publish
    @echo "==> Delivered {{app_version}} as {{deploy_tag}} to {{vm}}."

# Send images to the VM over SSH (compress → pipe → load)
deploy-pull-images:
    @echo "==> Sending {{deploy_web_image}} to {{vm}}..."
    {{container_cli}} save --compress {{deploy_web_image}} | ssh {{vm}} "sudo {{container_cli}} load"
    @echo "==> Sending {{deploy_silo_image}} to {{vm}}..."
    {{container_cli}} save --compress {{deploy_silo_image}} | ssh {{vm}} "sudo {{container_cli}} load"

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

# Full deploy: send images to the VM, run migrations, restart services
deploy: deploy-pull-images deploy-migrate deploy-restart-app
    @echo "==> Done. https://mostlyhuman.ca"
