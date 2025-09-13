# MediatR.Hangfire.Extensions Tests

This directory contains unit tests for the MediatR.Hangfire.Extensions library using MSTest and Coverlet for code coverage.

## Running Tests

### Basic Test Run

To run all tests without coverage:

```bash
cd tests
dotnet test
```

### Run Tests with Coverage

To run tests and generate code coverage reports:

```bash
cd tests
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

This will:

- Run all tests
- Generate a `coverage.cobertura.xml` file with coverage data
- Display coverage summary in the console

### Coverage Output

The coverage command displays a summary table showing:

- **Line Coverage**: Percentage of code lines executed by tests
- **Branch Coverage**: Percentage of code branches (if/else, switch cases) tested
- **Method Coverage**: Percentage of methods called by tests

Example output:

```
+-----------------------------+-------+--------+--------+
| Module                      | Line  | Branch | Method |
+-----------------------------+-------+--------+--------+
| MediatR.Hangfire.Extensions | 7.28% | 7.06%  | 20.4%  |
+-----------------------------+-------+--------+--------+
```

## Test Structure

Tests are organized by the component they test:

- `Configuration/` - Tests for configuration classes like `HangfireMediatorOptions`
- `Extensions/` - Tests for extension methods and builders

## Adding New Tests

To add tests for new components:

1. Create a new directory matching the namespace structure
2. Create test classes with the naming convention `{ComponentName}Tests.cs`
3. Use MSTest attributes: `[TestClass]`, `[TestMethod]`
4. Run coverage to see how your tests affect coverage metrics

## Dependencies

The test project uses:

- **MSTest**: Microsoft's testing framework
- **Coverlet**: Cross-platform code coverage tool that generates Cobertura XML reports
- **Microsoft.NET.Test.Sdk**: Required for running tests

## Coverage Files

- `coverage.cobertura.xml`: Generated coverage report in Cobertura XML format
