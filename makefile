.PHONY: build
build:
	dotnet build

amount = 8 # DEFAULT VALUE

.PHONY: test
test:
	dotnet run --project TesterEntry --amount $(amount)

.PHONY: index
index:
	dotnet run --project MoogleServer index
