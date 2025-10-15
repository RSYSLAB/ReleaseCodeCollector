# Release Code Collector

A .NET 9 console application that recursively scans directories for files, extracts their metadata and content, and stores the information in an MSSQL database using Dapper ORM. The application also tracks deployment releases with associated metadata.

## Features

- **Recursive File Discovery**: Scans all files and subdirectories under a specified path
- **Comprehensive File Information**: Collects file path, name, extension, size, timestamps, and content
- **Deployment Release Tracking**: Associates file collections with deployment releases including tags and deployment dates
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
dotnet run -- --source <path>
dotnet run -- -s <path>
```

### Full Named Arguments Syntax

```bash
dotnet run -- --source <path> [--connection <conn_string>] [--tags <tags>] [--deployment <deployment>] [--deployment-date <date>] [--batch-size <number>] [--max-file-size <bytes>] [--verbose] [--help]
```

### Examples

```bash
# Show help information
dotnet run -- --help
dotnet run -- -h

# Basic usage with named arguments
dotnet run -- --source C:\Projects
dotnet run -- -s C:\Projects

# With deployment tracking
 C:\Code --tags "v1.0,production" --deployment "Release-2025-10-15"

# With custom deployment date
dotnet run -- -s C:\Projects -t "v2.0,hotfix" -d "Hotfix-2025-10-16" -dd "2025-10-16 09:30:00"

# With custom connection string and verbose output
dotnet run -- --source C:\Code --connection "Server=myserver;Database=MyDB;User Id=user;Password=pass;" --verbose

# With custom batch size and max file size
dotnet run -- -s D:\Source -b 1000 -m 52428800 -v

# Backward compatibility - positional argument still works
dotnet run -- C:\Projects
# Scan current directory with default connection
dotnet run -- --source C:\Projects

# Scan with deployment information
dotnet run -- --source C:\MyCode --tags "v1.0,production" --deployment "Production-Release"

# Scan specific directory with custom database and deployment tracking
dotnet run -- --source C:\MyCode --connection "Server=prod-server;Database=CodeAnalysis;Integrated Security=true;" --tags "v2.1,staging" --deployment "Staging-2025-10-15"

# Scan and use local SQL Server Express with custom batch size and deployment date
dotnet run -- -s D:\Source -c "Server=localhost\SQLEXPRESS;Database=ReleaseCodeCollector;Integrated Security=true;TrustServerCertificate=true;" -b 250 -t "hotfix" -d "Emergency-Fix" -dd "2025-10-15 14:30:00"

# Full configuration with all options
dotnet run -- --source C:\Projects --connection "Server=myserver;Database=CodeDB;Integrated Security=true;" --batch-size 1000 --max-file-size 52428800 --tags "v3.0,release" --deployment "Major-Release-3.0" --deployment-date "2025-12-01 10:00:00" --verbose
```

## Command Line Arguments

| Argument            | Short | Description                      | Default                                                                                              | Required |
| ------------------- | ----- | -------------------------------- | ---------------------------------------------------------------------------------------------------- | -------- |
| `--source`          | `-s`  | Path to source directory to scan | -                                                                                                    | Yes      |
| `--tags`            | `-t`  | Tags to associate with this run  | -                                                                                                    | Yes      |
| `--deployment`      | `-d`  | Deployment identifier            | -                                                                                                    | Yes      |
| `--deployment-date` | `-dd` | Deployment date and time         | Current UTC date and time                                                                            | No       |
| `--connection`      | `-c`  | MSSQL connection string          | Server=localhost;Database=ReleaseCodeCollector;Integrated Security=true;TrustServerCertificate=true; | No       |
| `--batch-size`      | `-b`  | Files per database batch         | 500                                                                                                  | No       |
| `--max-file-size`   | `-m`  | Max file size to read (bytes)    | 104857600 (100MB)                                                                                    | No       |
| `--verbose`         | `-v`  | Enable verbose output            | false                                                                                                | No       |
| `--help`            | `-h`  | Show help information            | -                                                                                                    | No       |

**Note:** The application maintains backward compatibility with positional arguments where the first argument is treated as the source directory path.

### Environment Variable Support

The application supports environment variable expansion in command-line arguments for the `--source` and `--connection` parameters. This allows for flexible configuration across different environments.

**Supported Formats:**

- Windows style: `%VARIABLE_NAME%`
- Unix/Linux style: `$VARIABLE_NAME` or `${VARIABLE_NAME}`

**Examples:**

```bash
# Windows environment variables
dotnet run -- --source %PROJECT_PATH%
dotnet run -- -s %MY_SOURCE% -c "Server=%DB_SERVER%;Database=%DB_NAME%;Integrated Security=true;"

# Unix/Linux style variables
dotnet run -- --source $HOME/projects
dotnet run -- -s ${SOURCE_DIR} -c "Server=$DB_HOST;Database=$DB_NAME;"

# Mixed usage with deployment tracking
dotnet run -- --source %USERPROFILE%\Documents\Projects -c "Server=${DB_SERVER};Database=MyDB;" --tags "v1.0" --deployment "Release-${BUILD_NUMBER}"
```

**Environment Variable Examples:**

```powershell
# PowerShell - Set environment variables
$env:PROJECT_PATH = "C:\MyProjects"
$env:DB_SERVER = "localhost"
$env:DB_NAME = "CodeDatabase"

# Run with environment variables
dotnet run -- --source %PROJECT_PATH% --tags "production" --deployment "Release-${BUILD_VERSION}" --verbose
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
- **`DeploymentRelease`**: Record type representing deployment release information with tags and deployment dates

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
│   ├── DeploymentRelease.cs    # Deployment release model
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

The application automatically creates two main tables for storing file information and deployment release data:

### DEPLOYMENT_RELEASE_FILES Table

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

### DEPLOYMENT_RELEASE Table

| Column         | Type             | Description                              |
| -------------- | ---------------- | ---------------------------------------- |
| Id             | BIGINT IDENTITY  | Primary key                              |
| RunId          | UNIQUEIDENTIFIER | Links to the execution run               |
| Tags           | NVARCHAR(255)    | Comma-separated tags (e.g., v1.0,prod)   |
| Deployment     | NVARCHAR(255)    | Deployment identifier/name               |
| DeploymentDate | DATETIME2        | When the deployment was created/executed |
| ProcessedDate  | DATETIME2        | When the record was inserted             |

**Run Tracking**: Each execution of the application generates a unique `RunId` (UUID/GUID) that is assigned to all files processed during that run and links the deployment release information. This allows you to:

- Group files by execution run
- Track deployment releases with their associated files
- Compare results between different deployments
- Associate file changes with specific deployment versions
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
- Track changes between different deployment versions
- Compare file content across different releases
- Audit code changes for specific deployment releases

## Deployment Release Tracking

The Release Code Collector now includes comprehensive deployment release tracking capabilities, allowing you to associate file collections with specific deployment versions, tags, and timestamps.

### Key Features

- **Tagged Releases**: Associate deployments with custom tags (e.g., "v1.0", "production", "hotfix")
- **Deployment Identification**: Assign unique names to each deployment release
- **Timestamp Tracking**: Record when deployments were created or executed
- **Linked File Data**: All files processed in a run are automatically linked to the deployment release
- **Historical Analysis**: Compare files and content across different deployment versions

### Deployment Workflow

1. **Specify Deployment Information**: Use `--tags`, `--deployment`, and optionally `--deployment-date` parameters
2. **Process Files**: The application scans and processes files as usual
3. **Database Storage**: Files are stored in `DEPLOYMENT_RELEASE_FILES` and deployment info in `DEPLOYMENT_RELEASE`
4. **Query and Analysis**: Use SQL queries to analyze deployments, compare versions, and track changes

### Example Deployment Scenarios

```bash
# Production release
dotnet run -- --source C:\Projects\MyApp --tags "v2.1.0,production,stable" --deployment "Production-Release-2.1.0"

# Hotfix deployment
dotnet run -- -s C:\Projects\MyApp -t "v2.1.1,hotfix,critical" -d "Hotfix-SecurityPatch" -dd "2025-10-15 16:30:00"

# Staging deployment
dotnet run -- --source C:\Projects\MyApp --tags "v2.2.0-rc1,staging,release-candidate" --deployment "Staging-RC1"

# Development snapshot
dotnet run -- -s C:\Projects\MyApp -t "dev,feature-branch,experimental" -d "Dev-FeatureX-Snapshot"
```

### Useful Queries

```sql
-- Get all files from a specific run
SELECT * FROM DEPLOYMENT_RELEASE_FILES WHERE RunId = 'your-run-id-here';

-- Get deployment release information for a specific run
SELECT * FROM DEPLOYMENT_RELEASE WHERE RunId = 'your-run-id-here';

-- Get files and deployment information together
SELECT f.FullPath, f.FileName, f.FileExtension, f.FileSizeBytes,
       d.Tags, d.Deployment, d.DeploymentDate
FROM DEPLOYMENT_RELEASE_FILES f
INNER JOIN DEPLOYMENT_RELEASE d ON f.RunId = d.RunId
WHERE f.RunId = 'your-run-id-here';

-- Get summary statistics by deployment
SELECT d.Tags, d.Deployment, d.DeploymentDate,
       COUNT(f.Id) as FileCount,
       SUM(f.FileSizeBytes) as TotalSize,
       MIN(f.ProcessedDate) as RunStartTime,
       MAX(f.ProcessedDate) as RunEndTime
FROM DEPLOYMENT_RELEASE d
LEFT JOIN DEPLOYMENT_RELEASE_FILES f ON d.RunId = f.RunId
GROUP BY d.Tags, d.Deployment, d.DeploymentDate, d.RunId
ORDER BY d.DeploymentDate DESC;

-- Get the most recent deployment
SELECT TOP 1 * FROM DEPLOYMENT_RELEASE
ORDER BY DeploymentDate DESC;

-- Find deployments by tag
SELECT * FROM DEPLOYMENT_RELEASE
WHERE Tags LIKE '%production%'
ORDER BY DeploymentDate DESC;

-- Search for files containing specific text in their content
SELECT f.FullPath, f.FileName, f.FileExtension, f.FileSizeBytes,
       d.Tags, d.Deployment, d.DeploymentDate
FROM DEPLOYMENT_RELEASE_FILES f
INNER JOIN DEPLOYMENT_RELEASE d ON f.RunId = d.RunId
WHERE f.Content LIKE '%your-search-term%'
  AND f.IsReadable = 1;

-- Search for files containing a specific function name (case-insensitive)
SELECT f.FullPath, f.FileName, COUNT(*) as Occurrences,
       d.Tags, d.Deployment
FROM DEPLOYMENT_RELEASE_FILES f
INNER JOIN DEPLOYMENT_RELEASE d ON f.RunId = d.RunId
WHERE LOWER(f.Content) LIKE '%function myfunction%'
  AND f.IsReadable = 1
GROUP BY f.FullPath, f.FileName, d.Tags, d.Deployment
ORDER BY Occurrences DESC;

-- Find all files that contain database connection strings
SELECT f.FullPath, f.FileName, f.FileExtension,
       d.Tags, d.Deployment, d.DeploymentDate
FROM DEPLOYMENT_RELEASE_FILES f
INNER JOIN DEPLOYMENT_RELEASE d ON f.RunId = d.RunId
WHERE (f.Content LIKE '%connectionstring%'
   OR f.Content LIKE '%server=%'
   OR f.Content LIKE '%database=%')
  AND f.IsReadable = 1;

-- Compare file counts between different deployments
SELECT d.Deployment, d.Tags, d.DeploymentDate,
       COUNT(f.Id) as FileCount,
       SUM(f.FileSizeBytes) as TotalSizeBytes
FROM DEPLOYMENT_RELEASE d
LEFT JOIN DEPLOYMENT_RELEASE_FILES f ON d.RunId = f.RunId
GROUP BY d.Deployment, d.Tags, d.DeploymentDate, d.RunId
ORDER BY d.DeploymentDate DESC;

-- Find files by extension that contain specific patterns for a deployment
SELECT f.FullPath, f.FileName, f.FileSizeBytes,
       d.Deployment, d.Tags
FROM DEPLOYMENT_RELEASE_FILES f
INNER JOIN DEPLOYMENT_RELEASE d ON f.RunId = d.RunId
WHERE f.FileExtension IN ('.cs', '.js', '.ts', '.py')
  AND f.Content LIKE '%async%'
  AND f.IsReadable = 1
  AND d.Tags LIKE '%production%';
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
│   ├── FileInformation.cs       # File metadata and content model
│   ├── CommandLineOptions.cs   # Command-line options model
│   └── DeploymentRelease.cs    # Deployment release model
├── Services/
│   ├── FileDiscoveryService.cs  # File scanning and processing
│   ├── CommandLineService.cs    # Argument parsing and validation
│   └── DatabaseService.cs       # Database operations with Dapper
├── Tests/                       # Unit tests
│   ├── Models/                  # Model tests
│   └── Services/                # Service tests
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
