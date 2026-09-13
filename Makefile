.DEFAULT_GOAL := help

ENV_FILE ?= .env.development
COMPOSE = docker compose --env-file $(ENV_FILE)

PROD_ENV_FILE ?= .env.production
PROD_PROJECT ?= laoyu-blog-prod
PROD_COMPOSE = docker compose \
	-p $(PROD_PROJECT) \
	-f compose.production.yaml \
	--env-file $(PROD_ENV_FILE)

VPS_COMPOSE = docker compose \
	-p $(PROD_PROJECT) \
	-f compose.vps.yaml \
	--env-file $(PROD_ENV_FILE)

.PHONY: help build up dev down logs prod-up prod-down prod-logs prod-ps vps-up vps-down vps-logs vps-ps migration db-update db-rollback

help:
	@echo "Available commands:"
	@echo "  make build"
	@echo "  make up"
	@echo "  make dev"
	@echo "  make down"
	@echo "  make logs"
	@echo "  make prod-up"
	@echo "  make prod-down"
	@echo "  make prod-logs"
	@echo "  make prod-ps"
	@echo "  make vps-up"
	@echo "  make vps-down"
	@echo "  make vps-logs"
	@echo "  make vps-ps"
	@echo "  make migration NAME=InitialCreate"
	@echo "  make db-update"
	@echo "  make db-rollback TARGET=0"

build:
	dotnet build

up:
	$(COMPOSE) up --detach --build
	
dev:
	$(COMPOSE) up --watch --build

down:
	$(COMPOSE) down

logs:
	$(COMPOSE) logs --follow api

prod-up:
	$(PROD_COMPOSE) up --detach --build

prod-down:
	$(PROD_COMPOSE) down

prod-logs:
	$(PROD_COMPOSE) logs --follow

prod-ps:
	$(PROD_COMPOSE) ps

vps-up:
	$(VPS_COMPOSE) up --detach --build

vps-down:
	$(VPS_COMPOSE) down

vps-logs:
	$(VPS_COMPOSE) logs --follow

vps-ps:
	$(VPS_COMPOSE) ps

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
