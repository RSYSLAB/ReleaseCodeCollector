# Models

This directory contains all the data models used throughout the Release Code Collector application.

## Files

### `FileInformation.cs`

Contains the `FileInformation` record that represents comprehensive information about a file including its metadata and content. This is the primary data model used for storing file information in the database.

**Key Properties:**

- `RunId`: Unique identifier for the execution run
- `FullPath`: Complete file path
- `FileName`: File name with extension
- `Content`: Textual content of the file
- `ContentHash`: SHA256 hash for integrity verification

### `CommandLineOptions.cs`

Contains the `CommandLineOptions` record that represents parsed command-line arguments and configuration options.

**Key Properties:**

- `Directory`: Path to scan
- `ConnectionString`: Database connection string
- `BatchSize`: Number of files per batch
- `MaxFileSize`: Maximum file size to read
- `Verbose`: Enable verbose output
- `ShowHelp`: Display help information

## Usage

All models use the `ReleaseCodeCollector.Models` namespace and are designed as immutable records for thread safety and value semantics.

```csharp
using ReleaseCodeCollector.Models;

// Example usage
var options = new CommandLineOptions(...);
var fileInfo = new FileInformation(...);
```
