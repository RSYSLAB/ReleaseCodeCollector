# Test Documentation

This directory contains comprehensive unit tests for the Release Code Collector application.

## Test Structure

### Models Tests

- **`FileInformationTests.cs`**: Tests for the FileInformation record

  - Constructor validation
  - Property assignment
  - Equality/inequality operations
  - Null value handling
  - Edge cases and special characters

- **`CommandLineOptionsTests.cs`**: Tests for the CommandLineOptions record
  - Property assignment and validation
  - Equality operations
  - Null/empty string handling
  - Edge cases with max values

### Services Tests

- **`CommandLineServiceTests.cs`**: Comprehensive tests for command-line argument parsing

  - All argument formats (long and short)
  - Environment variable expansion (Windows %VAR%, Unix $VAR, braced ${VAR})
  - Validation logic
  - Error handling and exception scenarios
  - Configuration integration

- **`FileDiscoveryServiceTests.cs`**: Tests for file discovery and processing

  - File enumeration and metadata extraction
  - Binary file detection and handling
  - Large file handling
  - Subdirectory recursion
  - Content reading and hash generation
  - Error scenarios and cancellation

- **`DatabaseServiceTests.cs`**: Tests for database operations
  - Parameter validation
  - Null handling
  - Data preparation
  - Batch processing logic
  - Error scenarios

## Running Tests

### Command Line

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests with detailed output
dotnet test --logger:console --verbosity:detailed

# Run specific test class
dotnet test --filter:ClassName=FileInformationTests

# Run tests with settings
dotnet test --settings:Tests/test.runsettings
```

### Visual Studio

- Open Test Explorer (Test → Test Explorer)
- Build the solution
- Run all tests or specific test classes

## Test Categories

### Unit Tests

- Fast-running tests that don't require external dependencies
- Mock/fake objects for dependencies
- Focus on individual class/method behavior

### Edge Cases Covered

- Null and empty inputs
- Special characters in file names and content
- Large data sets
- Unicode content
- Binary file handling
- Network timeouts and cancellation
- Invalid configurations

## Code Coverage Goals

- **Models**: 100% coverage (simple records, straightforward to test)
- **Services**: 90%+ coverage
- **Overall**: 85%+ coverage

## Test Data Management

- Tests use temporary directories that are cleaned up automatically
- No dependency on external files or databases for unit tests
- Environment variables are set/cleaned in test scope
- Mock configurations using in-memory providers

## Best Practices

1. **Arrange-Act-Assert**: Clear test structure
2. **Descriptive Names**: Test names describe the scenario and expected outcome
3. **Single Responsibility**: Each test focuses on one behavior
4. **Independence**: Tests don't depend on each other
5. **Cleanup**: Proper disposal of resources and temporary files
6. **Fast Execution**: Unit tests run quickly without external dependencies

## Integration Tests

For database integration tests, see separate integration test project or documentation.
These tests require:

- SQL Server instance
- Test database setup
- Connection string configuration
