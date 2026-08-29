DOCKER ?= docker

.PHONY: default build start stop clean

default: start ## By default, just start the container

build: ## Build the Docker image
	@$(DOCKER) compose build

start: ## Start the container via docker compose (docker compose builds automatically if necessary)
	@$(DOCKER) compose up -d

run:
	$(MAKE) start
	sleep 2
	open http://127.0.0.1:9664/

stop: ## Tear down the container via docker compose
	@$(DOCKER) compose down

clean: stop ## Remove the project containers and docker image
	@if $(DOCKER) image inspect r2wars:latest >/dev/null 2>&1; then \
		$(DOCKER) image rm r2wars:latest; \
	fi
