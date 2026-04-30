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
	docker-compose logs -f postgres	
