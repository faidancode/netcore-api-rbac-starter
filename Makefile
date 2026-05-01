.PHONY: run migrate seed watch

run:
	dotnet run --launch-profile http

migrate:
	dotnet run -- --migrate

seed:
	dotnet run -- --seed

watch:
	dotnet watch run --launch-profile http


up:
	docker-compose up -d

down:
	docker-compose down -v

logs:
	docker-compose logs -f

# 🐘 Start infrastructure only (Postgres + Redis)
infra:
	docker-compose up -d postgres redis

# 🛑 Stop infrastructure only
infra-down:
	docker-compose stop postgres redis

# 🔍 Infra logs (postgres + redis)
infra-logs:
	docker-compose logs -f postgres redis
