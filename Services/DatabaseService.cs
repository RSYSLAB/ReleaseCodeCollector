using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ReleaseCodeCollector.Models;
using System.Data;

namespace ReleaseCodeCollector.Services;

/// <summary>
/// Service responsible for database operations with MSSQL using Dapper.
/// </summary>
public class DatabaseService : IDatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService>? _logger;

    // SQL constants for better maintainability
    private const string CreateTablesScript = """
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DEPLOYMENT_RELEASE_FILES' AND xtype='U')
        CREATE TABLE DEPLOYMENT_RELEASE_FILES (
            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
            RunId UNIQUEIDENTIFIER NOT NULL,
            FullPath VARCHAR(4000) NOT NULL,
            FileName VARCHAR(255) NOT NULL,
            FileExtension VARCHAR(50),
            DirectoryPath VARCHAR(4000),
            FileSizeBytes BIGINT NOT NULL,
            CreatedDate DATETIME2 NOT NULL,
            ModifiedDate DATETIME2 NOT NULL,
            AccessedDate DATETIME2 NOT NULL,
            Content NVARCHAR(MAX),
            ContentHash VARCHAR(100),
            IsReadable BIT NOT NULL,
            ErrorMessage VARCHAR(1000),
            ProcessedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
            FullPathHash AS CONVERT(VARCHAR(100), HASHBYTES('SHA1', FullPath), 2),
            INDEX IX_DeploymentReleaseFiles_RunId NONCLUSTERED (RunId),
            INDEX IX_DeploymentReleaseFiles_FileExtension NONCLUSTERED (FileExtension),
            INDEX IX_DeploymentReleaseFiles_ModifiedDate NONCLUSTERED (ModifiedDate),
            INDEX IX_DeploymentReleaseFiles_FullPathHash NONCLUSTERED (FullPathHash)
        );
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DEPLOYMENT_RELEASE' AND xtype='U')
        CREATE TABLE DEPLOYMENT_RELEASE (
            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
            RunId UNIQUEIDENTIFIER NOT NULL,
            Tags VARCHAR(512) NOT NULL,
            Deployment VARCHAR(255) NOT NULL,
            DeploymentDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
            ProcessedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
            INDEX IX_DeploymentRelease_RunId NONCLUSTERED (RunId),
            INDEX IX_DeploymentRelease_Deployment NONCLUSTERED (Deployment),
            INDEX IX_DeploymentRelease_Tags NONCLUSTERED (Tags)
        );
        """;

    private const string InsertFileInformationScript = """
        INSERT INTO DEPLOYMENT_RELEASE_FILES
        (RunId, FullPath, FileName, FileExtension, DirectoryPath, FileSizeBytes, 
         CreatedDate, ModifiedDate, AccessedDate, Content, ContentHash, 
         IsReadable, ErrorMessage)
        VALUES 
        (@RunId, @FullPath, @FileName, @FileExtension, @DirectoryPath, @FileSizeBytes, 
         @CreatedDate, @ModifiedDate, @AccessedDate, @Content, @ContentHash, 
         @IsReadable, @ErrorMessage);
        """;

    private const string InsertDeploymentReleaseScript = """
        INSERT INTO DEPLOYMENT_RELEASE
        (RunId, Tags, Deployment, DeploymentDate)
        VALUES 
        (@RunId, @Tags, @Deployment, @DeploymentDate);
        """;

    private const string SelectDeploymentReleaseByRunIdScript = """
        SELECT RunId, Tags, Deployment, DeploymentDate
        FROM DEPLOYMENT_RELEASE
        WHERE RunId = @RunId
        ORDER BY DeploymentDate DESC;
        """;

    private const string SelectFileInformationByRunIdScript = """
        SELECT RunId, FullPath, FileName, FileExtension, DirectoryPath, FileSizeBytes,
               CreatedDate, ModifiedDate, AccessedDate, Content, ContentHash,
               IsReadable, ErrorMessage
        FROM DEPLOYMENT_RELEASE_FILES
        WHERE RunId = @RunId
        ORDER BY FullPath;
        """;

    private const string CountFilesByRunIdScript = """
        SELECT COUNT(*)
        FROM DEPLOYMENT_RELEASE_FILES
        WHERE RunId = @RunId;
        """;

    /// <summary>
    /// Initializes a new instance of the DatabaseService.
    /// </summary>
    /// <param name="connectionString">The MSSQL connection string</param>
    /// <param name="logger">Optional logger for diagnostics</param>
    /// <exception cref="ArgumentException">Thrown when connection string is null or empty</exception>
    public DatabaseService(string connectionString, ILogger<DatabaseService>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
        _logger = logger;
    }

    /// <summary>
    /// Creates the table structure for storing file information if it doesn't exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Initializing database tables...");

            using var connection = await CreateConnectionAsync(cancellationToken);
            await connection.ExecuteAsync(CreateTablesScript);

            _logger?.LogInformation("Database tables initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize database tables");
            throw;
        }
    }

    /// <summary>
    /// Creates and opens a new SQL connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>An opened SQL connection</returns>
    private async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    /// <summary>
    /// Executes a database operation within a transaction.
    /// </summary>
    /// <typeparam name="T">The return type of the operation</typeparam>
    /// <param name="operation">The database operation to execute</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The result of the operation</returns>
    private async Task<T> ExecuteWithTransactionAsync<T>(
        Func<IDbConnection, IDbTransaction, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        using var connection = await CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var result = await operation(connection, transaction);
            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Inserts file information records into the database using bulk operations.
    /// </summary>
    /// <param name="fileInformationList">Collection of file information to insert</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The number of records inserted</returns>
    public async Task<int> InsertFileInformationAsync(IEnumerable<FileInformation> fileInformationList, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileInformationList);

        var fileList = fileInformationList.ToList();
        if (fileList.Count == 0)
        {
            _logger?.LogInformation("No file information to insert");
            return 0;
        }

        try
        {
            _logger?.LogInformation("Inserting {Count} file information records...", fileList.Count);

            var rowsAffected = await ExecuteWithTransactionAsync(async (connection, transaction) =>
                await connection.ExecuteAsync(InsertFileInformationScript, fileList, transaction),
                cancellationToken);

            _logger?.LogInformation("Successfully inserted {Count} file information records", rowsAffected);
            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to insert file information records");
            throw;
        }
    }

    /// <summary>
    /// Inserts file information records in batches to handle large datasets efficiently.
    /// </summary>
    /// <param name="fileInformationAsyncEnumerable">Async enumerable of file information</param>
    /// <param name="batchSize">Number of records to process in each batch</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The total number of records inserted</returns>
    public async Task<int> InsertFileInformationBatchAsync(
        IAsyncEnumerable<FileInformation> fileInformationAsyncEnumerable,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileInformationAsyncEnumerable);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);

        var totalInserted = 0;
        var batch = new List<FileInformation>(batchSize);

        try
        {
            _logger?.LogInformation("Starting batch insertion with batch size {BatchSize}", batchSize);

            await foreach (var fileInfo in fileInformationAsyncEnumerable.WithCancellation(cancellationToken))
            {
                batch.Add(fileInfo);

                if (batch.Count >= batchSize)
                {
                    var inserted = await InsertFileInformationAsync(batch, cancellationToken);
                    totalInserted += inserted;
                    batch.Clear();

                    _logger?.LogInformation("Processed {TotalInserted} files...", totalInserted);
                }
            }

            // Insert remaining items
            if (batch.Count > 0)
            {
                var inserted = await InsertFileInformationAsync(batch, cancellationToken);
                totalInserted += inserted;
            }

            _logger?.LogInformation("Batch insertion completed. Total records inserted: {TotalInserted}", totalInserted);
            return totalInserted;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed during batch insertion. Total inserted before error: {TotalInserted}", totalInserted);
            throw;
        }
    }

    /// <summary>
    /// Inserts a deployment release record into the database.
    /// </summary>
    /// <param name="deploymentRelease">The deployment release information to insert</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The number of records inserted (should be 1 on success)</returns>
    public async Task<int> InsertDeploymentReleaseAsync(DeploymentRelease deploymentRelease, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(deploymentRelease);

        try
        {
            _logger?.LogInformation("Inserting deployment release for RunId {RunId}", deploymentRelease.RunId);

            var rowsAffected = await ExecuteWithTransactionAsync(async (connection, transaction) =>
                await connection.ExecuteAsync(InsertDeploymentReleaseScript, deploymentRelease, transaction),
                cancellationToken);

            _logger?.LogInformation("Successfully inserted deployment release record");
            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to insert deployment release for RunId {RunId}", deploymentRelease.RunId);
            throw;
        }
    }

    /// <summary>
    /// Tests the database connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if connection is successful, false otherwise</returns>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Testing database connection...");

            using var connection = await CreateConnectionAsync(cancellationToken);
            // Execute a simple query to verify connection
            await connection.QuerySingleAsync<int>("SELECT 1");

            _logger?.LogInformation("Database connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Database connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Retrieves deployment releases by run ID.
    /// </summary>
    /// <param name="runId">The run ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of deployment releases</returns>
    public async Task<IEnumerable<DeploymentRelease>> GetDeploymentReleasesByRunIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Retrieving deployment releases for RunId {RunId}", runId);

            using var connection = await CreateConnectionAsync(cancellationToken);
            var results = await connection.QueryAsync<DeploymentRelease>(
                SelectDeploymentReleaseByRunIdScript,
                new
                {
                    RunId = runId
                });

            var deploymentReleases = results.ToList();
            _logger?.LogInformation("Retrieved {Count} deployment releases for RunId {RunId}", deploymentReleases.Count, runId);
            return deploymentReleases;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to retrieve deployment releases for RunId {RunId}", runId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves file information by run ID.
    /// </summary>
    /// <param name="runId">The run ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of file information</returns>
    public async Task<IEnumerable<FileInformation>> GetFileInformationByRunIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Retrieving file information for RunId {RunId}", runId);

            using var connection = await CreateConnectionAsync(cancellationToken);
            var results = await connection.QueryAsync<FileInformation>(
                SelectFileInformationByRunIdScript,
                new
                {
                    RunId = runId
                });

            var fileInformation = results.ToList();
            _logger?.LogInformation("Retrieved {Count} file information records for RunId {RunId}", fileInformation.Count, runId);
            return fileInformation;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to retrieve file information for RunId {RunId}", runId);
            throw;
        }
    }

    /// <summary>
    /// Gets the count of files for a specific run ID.
    /// </summary>
    /// <param name="runId">The run ID to count files for</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The count of files</returns>
    public async Task<int> GetFileCountByRunIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Getting file count for RunId {RunId}", runId);

            using var connection = await CreateConnectionAsync(cancellationToken);
            var count = await connection.QuerySingleAsync<int>(
                CountFilesByRunIdScript,
                new
                {
                    RunId = runId
                });

            _logger?.LogInformation("File count for RunId {RunId}: {Count}", runId, count);
            return count;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get file count for RunId {RunId}", runId);
            throw;
        }
    }
}