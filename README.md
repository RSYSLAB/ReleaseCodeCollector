# Release Code Collector

A .NET 9 console application that recursively scans directories for files, extracts their metadata and content, and stores the information in an MSSQL database using Dapper ORM.

## Features

- **Recursive File Discovery**: Scans all files and subdirectories under a specified path
- **Comprehensive File Information**: Collects file path, name, extension, size, timestamps, and content
- **Run Tracking**: Each execution generates a unique UUID to group all files processed in that run
- **Smart Content Reading**: Automatically detects binary files and large files to avoid reading non-text content
- **Database Integration**: Uses Dapper ORM with MSSQL for efficient data storage
- **Named Command Arguments**: Supports both long and short form command-line arguments with backward compatibility
- **Environment Variable Support**: Command-line arguments support Windows (%VAR%) and Unix ($VAR, ${VAR}) environment variable expansion
- **Batch Processing**: Processes files in configurable batches for optimal performance
- **Service-Based Architecture**: Clean separation of concerns with dedicated services for different responsibilities
- **Error Handling**: Robust error handling with detailed logging
- **Modern .NET Features**: Uses .NET 9 with records, async enumerables, and modern C# features

## Prerequisites

- .NET 9.0 or later
- MSSQL Server (local or remote)
- Appropriate database permissions for creating tables and inserting data

## Installation

1. Clone or download the source code
2. Navigate to the project directory
3. Restore dependencies:
   ```
   dotnet restore
   ```
4. Build the project:
   ```
   dotnet build
   ```

## Usage

The application now supports named command-line arguments for better usability and clarity.

### Basic Usage with Named Arguments

```bash
dotnet run -- --directory <path>
dotnet run -- -d <path>
```

### Backward Compatibility (Positional Arguments)

```bash
dotnet run -- <directory_path>
```

### Full Named Arguments Syntax

```bash
dotnet run -- --directory <path> [--connection <conn_string>] [--batch-size <number>] [--max-file-size <bytes>] [--verbose] [--help]
```

### Examples

```bash
# Show help information
dotnet run -- --help
dotnet run -- -h

# Basic usage with named arguments
dotnet run -- --directory C:\Projects
dotnet run -- -d C:\Projects

# With custom connection string and verbose output
dotnet run -- --directory C:\Code --connection "Server=myserver;Database=MyDB;User Id=user;Password=pass;" --verbose

# With custom batch size and max file size
dotnet run -- -d D:\Source -b 1000 -m 52428800 -v

# Backward compatibility - positional argument still works
dotnet run -- C:\Projects
# Scan current directory with default connection
dotnet run -- --directory C:\Projects

# Scan specific directory with custom database
dotnet run -- --directory C:\MyCode --connection "Server=prod-server;Database=CodeAnalysis;Integrated Security=true;"

# Scan and use local SQL Server Express with custom batch size
dotnet run -- -d D:\Source -c "Server=localhost\SQLEXPRESS;Database=ReleaseCodeCollector;Integrated Security=true;TrustServerCertificate=true;" -b 250

# Full configuration with all options
dotnet run -- --directory C:\Projects --connection "Server=myserver;Database=CodeDB;Integrated Security=true;" --batch-size 1000 --max-file-size 52428800 --verbose
```

## Command Line Arguments

| Argument          | Short | Description                   | Default                                                                                              | Required |
| ----------------- | ----- | ----------------------------- | ---------------------------------------------------------------------------------------------------- | -------- |
| `--directory`     | `-d`  | Path to directory to scan     | -                                                                                                    | Yes      |
| `--connection`    | `-c`  | MSSQL connection string       | Server=localhost;Database=ReleaseCodeCollector;Integrated Security=true;TrustServerCertificate=true; | No       |
| `--batch-size`    | `-b`  | Files per database batch      | 500                                                                                                  | No       |
| `--max-file-size` | `-m`  | Max file size to read (bytes) | 104857600 (100MB)                                                                                    | No       |
| `--verbose`       | `-v`  | Enable verbose output         | false                                                                                                | No       |
| `--help`          | `-h`  | Show help information         | -                                                                                                    | No       |

**Note:** The application maintains backward compatibility with positional arguments where the first argument is treated as the directory path.

### Environment Variable Support

The application supports environment variable expansion in command-line arguments for the `--directory` and `--connection` parameters. This allows for flexible configuration across different environments.

**Supported Formats:**

- Windows style: `%VARIABLE_NAME%`
- Unix/Linux style: `$VARIABLE_NAME` or `${VARIABLE_NAME}`

**Examples:**

```bash
# Windows environment variables
dotnet run -- --directory %PROJECT_PATH%
dotnet run -- -d %MY_SOURCE% -c "Server=%DB_SERVER%;Database=%DB_NAME%;Integrated Security=true;"

# Unix/Linux style variables
dotnet run -- --directory $HOME/projects
dotnet run -- -d ${SOURCE_DIR} -c "Server=$DB_HOST;Database=$DB_NAME;"

# Mixed usage
dotnet run -- --directory %USERPROFILE%\Documents\Projects -c "Server=${DB_SERVER};Database=MyDB;"
```

**Environment Variable Examples:**

```powershell
# PowerShell - Set environment variables
$env:PROJECT_PATH = "C:\MyProjects"
$env:DB_SERVER = "localhost"
$env:DB_NAME = "CodeDatabase"

# Run with environment variables
dotnet run -- --directory %PROJECT_PATH% --verbose
```

## Architecture

The application follows a clean, service-based architecture with clear separation of concerns:

### Services

- **`CommandLineService`**: Handles all command-line argument parsing, validation, and environment variable expansion
- **`FileDiscoveryService`**: Responsible for recursively discovering files and extracting their metadata and content
- **`DatabaseService`**: Manages all database operations using Dapper ORM, including table creation and batch inserts

### Models

- **`FileInformation`**: Record type representing file metadata and content with RunId tracking
- **`CommandLineOptions`**: Record type representing parsed command-line arguments and configuration options

### Key Benefits

- **Maintainability**: Each service has a single responsibility, making the code easier to maintain and test
- **Testability**: Services can be unit tested in isolation with dependency injection
- **Extensibility**: New features can be added by extending existing services or adding new ones
- **Reusability**: Services can be reused in different contexts or applications
- **Clean Models**: Data models are separated into dedicated files for better organization

### Project Structure

```
ReleaseCodeCollector/
├── Models/
│   ├── FileInformation.cs      # File metadata and content model
│   ├── CommandLineOptions.cs   # Command-line options model
│   └── README.md               # Models documentation
├── Services/
│   ├── CommandLineService.cs   # Argument parsing and validation
│   ├── FileDiscoveryService.cs # File discovery and processing
│   └── DatabaseService.cs      # Database operations
└── Program.cs                  # Application entry point
```

### Service Dependencies

```
Program.cs
├── CommandLineService (configuration)
├── FileDiscoveryService
└── DatabaseService (connection string)
```

## Database Schema

The application automatically creates a `ReleaseCodeFiles` table with the following structure:

| Column        | Type             | Description                              |
| ------------- | ---------------- | ---------------------------------------- |
| Id            | BIGINT IDENTITY  | Primary key                              |
| RunId         | UNIQUEIDENTIFIER | Unique identifier for each execution run |
| FullPath      | NVARCHAR(4000)   | Complete file path                       |
| FileName      | NVARCHAR(255)    | File name with extension                 |
| FileExtension | NVARCHAR(50)     | File extension                           |
| DirectoryPath | NVARCHAR(4000)   | Directory containing the file            |
| FileSizeBytes | BIGINT           | File size in bytes                       |
| CreatedDate   | DATETIME2        | File creation timestamp                  |
| ModifiedDate  | DATETIME2        | Last modification timestamp              |
| AccessedDate  | DATETIME2        | Last access timestamp                    |
| Content       | NVARCHAR(MAX)    | File content (if text file)              |
| ContentHash   | NVARCHAR(100)    | SHA256 hash of content                   |
| IsReadable    | BIT              | Whether content was successfully read    |
| ErrorMessage  | NVARCHAR(1000)   | Error message if processing failed       |
| ProcessedDate | DATETIME2        | When the record was inserted             |

**Run Tracking**: Each execution of the application generates a unique `RunId` (UUID/GUID) that is assigned to all files processed during that run. This allows you to:

- Group files by execution run
- Track when specific collections were performed
- Compare results between different runs
- Clean up data from specific runs if needed

## Content Search Capabilities

One of the powerful features of the Release Code Collector is the ability to search through file content stored in the database. Since the application reads and stores the textual content of files, you can perform sophisticated searches across your entire codebase using SQL queries.

**Search Features:**

- **Text Pattern Matching**: Use `LIKE` operators to find specific strings, functions, or patterns
- **Case-Insensitive Search**: Use `LOWER()` function for case-insensitive searches
- **Multi-term Search**: Combine multiple search conditions to find complex patterns
- **File Type Filtering**: Search within specific file extensions (e.g., only .cs files)
- **Occurrence Counting**: Count how many times a pattern appears in files
- **Full-Text Search**: For advanced scenarios, you can enable SQL Server full-text indexing

**Use Cases:**

- Find all files containing specific function names or API calls
- Locate database connection strings or configuration patterns
- Search for TODO comments or technical debt markers
- Identify files using deprecated libraries or patterns
- Find security-sensitive code patterns
- Analyze code patterns across multiple projects

### Useful Queries

```sql
-- Get all files from a specific run
SELECT * FROM ReleaseCodeFiles WHERE RunId = 'your-run-id-here';

-- Get summary statistics by run
SELECT RunId, COUNT(*) as FileCount,
       SUM(FileSizeBytes) as TotalSize,
       MIN(ProcessedDate) as RunStartTime,
       MAX(ProcessedDate) as RunEndTime
FROM ReleaseCodeFiles
GROUP BY RunId;

-- Get the most recent run
SELECT TOP 1 RunId FROM ReleaseCodeFiles
ORDER BY ProcessedDate DESC;

-- Search for files containing specific text in their content
SELECT FullPath, FileName, FileExtension, FileSizeBytes
FROM ReleaseCodeFiles
WHERE Content LIKE '%your-search-term%'
  AND IsReadable = 1;

-- Search for files containing a specific function name (case-insensitive)
SELECT FullPath, FileName, COUNT(*) as Occurrences
FROM ReleaseCodeFiles
WHERE LOWER(Content) LIKE '%function myfunction%'
  AND IsReadable = 1
GROUP BY FullPath, FileName
ORDER BY Occurrences DESC;

-- Find all files that contain database connection strings
SELECT FullPath, FileName, FileExtension
FROM ReleaseCodeFiles
WHERE Content LIKE '%connectionstring%'
   OR Content LIKE '%server=%'
   OR Content LIKE '%database=%'
  AND IsReadable = 1;

-- Search for TODO comments across all files
SELECT FullPath, FileName, FileExtension,
       LEN(Content) - LEN(REPLACE(LOWER(Content), 'todo', '')) as TodoCount
FROM ReleaseCodeFiles
WHERE LOWER(Content) LIKE '%todo%'
  AND IsReadable = 1
ORDER BY TodoCount DESC;

-- Find files by extension that contain specific patterns
SELECT FullPath, FileName, FileSizeBytes
FROM ReleaseCodeFiles
WHERE FileExtension IN ('.cs', '.js', '.ts', '.py')
  AND Content LIKE '%async%'
  AND IsReadable = 1;

-- Full-text search for multiple terms (requires full-text indexing)
-- Note: You may need to enable full-text search on the Content column
SELECT FullPath, FileName, FileExtension
FROM ReleaseCodeFiles
WHERE CONTAINS(Content, '"database" AND "connection"')
  AND IsReadable = 1;
```

## Configuration

Default settings can be modified in `appsettings.json`:

- **BatchSize**: Number of files to process in each database batch (default: 500)
- **MaxFileSizeBytes**: Maximum file size to read content from (default: 100MB)
- **BinaryExtensions**: File extensions to treat as binary files

## Performance Considerations

- Files larger than 100MB are not read for content to avoid memory issues
- Binary files (executables, images, videos, etc.) are skipped for content reading
- Database operations are batched to improve performance
- Progress is reported during processing

## Error Handling

The application handles various error scenarios:

- Inaccessible files or directories
- Database connection issues
- File reading permissions
- Large file processing
- Binary file detection

Errors are logged with descriptive messages, and processing continues for other files.

## Development

### Project Structure

```
ReleaseCodeCollector/
├── Models/
│   └── FileInfo.cs              # File information data model
├── Services/
│   ├── FileDiscoveryService.cs  # File scanning and processing
│   └── DatabaseService.cs       # Database operations with Dapper
├── Program.cs                   # Main application entry point
├── appsettings.json            # Configuration settings
└── ReleaseCodeCollector.csproj # Project file
```

### Key Dependencies

- **Microsoft.Data.SqlClient**: MSSQL database connectivity
- **Dapper**: Lightweight ORM for database operations
- **.NET 9**: Modern C# features and performance improvements

## License

This project is provided as-is for educational and development purposes.
