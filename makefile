.PHONY: build
build:
	dotnet build

.PHONY: dev
dev:
	dotnet watch run --project MoogleServer

.PHONY: index
index:
	dotnet run --project MoogleServer index
