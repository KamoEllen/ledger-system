# ─────────────────────────────────────────────────────────────────────────────
# Ledger System — Makefile
# Common development tasks. Requires Docker, Docker Compose, and (optionally) .NET 8 SDK.
# ─────────────────────────────────────────────────────────────────────────────

.PHONY: help up down build logs ps \
        test unit integration \
        migrate seed shell-db \
        clean lint

# ── Configuration ─────────────────────────────────────────────────────────────
COMPOSE        := docker compose
API_SERVICE    := api
DB_SERVICE     := db
PROJECT        := LedgerSystem.sln

# ── Help (default target) ──────────────────────────────────────────────────────
help:
	@echo ""
	@echo "  Ledger System — available targets"
	@echo ""
	@echo "  Docker:"
	@echo "    make up          Start all services (build if needed)"
	@echo "    make down        Stop and remove containers"
	@echo "    make build       Rebuild the API image"
	@echo "    make logs        Follow API logs"
	@echo "    make ps          Show running containers"
	@echo ""
	@echo "  Testing:"
	@echo "    make test        Run all tests (unit + integration)"
	@echo "    make unit        Run unit tests only"
	@echo "    make integration Run integration tests only"
	@echo ""
	@echo "  Database:"
	@echo "    make migrate     Apply EF Core migrations (requires running DB)"
	@echo "    make seed        Seed the database with initial data"
	@echo "    make shell-db    Open psql shell inside the DB container"
	@echo ""
	@echo "  Utilities:"
	@echo "    make clean       Remove build artefacts"
	@echo ""

# ── Docker ─────────────────────────────────────────────────────────────────────
up:
	@cp -n .env.example .env 2>/dev/null || true
	$(COMPOSE) up --build -d
	@echo "\nAPI running at http://localhost:$${API_PORT:-8080}"
	@echo "Swagger UI at  http://localhost:$${API_PORT:-8080}/swagger\n"

down:
	$(COMPOSE) down

build:
	$(COMPOSE) build $(API_SERVICE)

logs:
	$(COMPOSE) logs -f $(API_SERVICE)

ps:
	$(COMPOSE) ps

# ── Testing ────────────────────────────────────────────────────────────────────
test: unit integration

unit:
	dotnet test tests/LedgerSystem.UnitTests/LedgerSystem.UnitTests.csproj \
		--configuration Release \
		--logger "console;verbosity=normal"

integration:
	dotnet test tests/LedgerSystem.IntegrationTests/LedgerSystem.IntegrationTests.csproj \
		--configuration Release \
		--logger "console;verbosity=normal"

# ── Database ───────────────────────────────────────────────────────────────────
migrate:
	$(COMPOSE) exec $(API_SERVICE) dotnet ef database update \
		--project src/LedgerSystem.Infrastructure \
		--startup-project src/LedgerSystem.API

seed:
	$(COMPOSE) exec $(API_SERVICE) dotnet LedgerSystem.API.dll --seed

shell-db:
	$(COMPOSE) exec $(DB_SERVICE) psql -U $${DB_USER:-ledger_user} -d $${DB_NAME:-ledger_db}

# ── Utilities ──────────────────────────────────────────────────────────────────
clean:
	find . -type d -name bin  -not -path "./.git/*" | xargs rm -rf
	find . -type d -name obj  -not -path "./.git/*" | xargs rm -rf
	find . -type d -name TestResults | xargs rm -rf
	@echo "Build artefacts removed."
