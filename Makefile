DOCKER ?= docker
WASM_ARCHIVE ?= r2wars-wasm.zip

.PHONY: default build start stop clean wasm-build wasm-test wasm-run wasm-dist

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

wasm-build: ## Build the standalone browser/WebAssembly version
	cd wasm && npm install && npm run build

wasm-test: ## Test the browser engine against radare2 WebAssembly
	cd wasm && npm install && npm test

wasm-run: ## Run the standalone browser/WebAssembly development server
	cd wasm && npm install && npm run dev

wasm-dist: wasm-build ## Build a static-host-ready WebAssembly zip archive
	cd wasm/dist && zip -qrFS "../$(WASM_ARCHIVE)" .
	@echo "Created wasm/$(WASM_ARCHIVE)"
