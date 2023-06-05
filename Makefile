DOCKER ?= sudo docker

.PHONY: default build start stop clean

default: start ## By default, just start the container

build: ## Build the Docker image
	@$(DOCKER) compose build

start: ## Start the container via docker compose (docker compose builds automatically if necessary)
	@$(DOCKER) compose up -d

stop: ## Tear down the container via docker compose
	@$(DOCKER) compose down

clean: ## Remove the docker image
	@$(DOCKER) rmi r2wars:latest

