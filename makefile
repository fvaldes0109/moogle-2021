.PHONY: build
build:
	dotnet build

.PHONY: dev
dev:
	dotnet watch run --project MoogleServer

amount = 8 # DEFAULT VALUE

.PHONY: test
test:
	dotnet run --project TesterEntry --amount $(amount)
