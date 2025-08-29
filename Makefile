SHELL := /bin/bash
.DEFAULT_GOAL := up

up:
	@docker compose up -d --build --remove-orphans

start:
	@docker compose up -d --remove-orphans

logs:
	@docker compose logs -f --tail=200 api ui

api-logs:
	@docker compose logs -f --tail=200 api

ui-logs:
	@docker compose logs -f --tail=200 ui

ps:
	@docker compose ps

stop:
	@docker compose down

reset:
	@docker compose down -v --remove-orphans || true
	@docker rm -f sql2022 2>/dev/null || true

first:
	@$(MAKE) reset && $(MAKE) up && $(MAKE) logs

test:
	dotnet test api/Checklist.Api.Tests/Checklist.Api.Tests.csproj