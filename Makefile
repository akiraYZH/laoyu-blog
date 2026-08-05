.DEFAULT_GOAL := help

ENV_FILE ?= .env.development
COMPOSE = docker compose --env-file $(ENV_FILE)

.PHONY: help build up dev down logs migration db-update db-rollback

help:
	@echo "Available commands:"
	@echo "  make build"
	@echo "  make up"
	@echo "  make dev"
	@echo "  make down"
	@echo "  make logs"
	@echo "  make migration NAME=InitialCreate"
	@echo "  make db-update"
	@echo "  make db-rollback TARGET=0"

build:
	dotnet build

up:
	$(COMPOSE) up --detach --build
	
dev:
	$(COMPOSE) watch

down:
	$(COMPOSE) down

logs:
	$(COMPOSE) logs --follow api

migration:
	@test -n "$(NAME)" || \
		(echo "Usage: make migration NAME=InitialCreate"; exit 1)
	./scripts/add-migration.sh "$(NAME)"

db-update:
	./scripts/update-database.sh

db-rollback:
	@test -n "$(TARGET)" || \
		(echo "Usage: make db-rollback TARGET=InitialCreate"; exit 1)
	./scripts/update-database.sh "$(TARGET)"
