using Dapper;
using Microsoft.Data.SqlClient;
using ReleaseCodeCollector.Models;

namespace ReleaseCodeCollector.Services;

/// <summary>
/// Service responsible for database operations with MSSQL using Dapper.
/// </summary>
public class DatabaseService : IDisposable
{
    private readonly string _connectionString;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the DatabaseService.
    /// </summary>
    /// <param name="connectionString">The MSSQL connection string</param>
    /// <exception cref="ArgumentException">Thrown when connection string is null or empty</exception>
    public DatabaseService(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <summary>
    /// Creates the table structure for storing file information if it doesn't exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        const string createTableSql = """
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DEPLOYMENT_RELEASE_FILES' AND xtype='U')
            CREATE TABLE DEPLOYMENT_RELEASE_FILES (
                Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                RunId UNIQUEIDENTIFIER NOT NULL,
                FullPath NVARCHAR(4000) NOT NULL,
                FileName NVARCHAR(255) NOT NULL,
                FileExtension NVARCHAR(50),
                DirectoryPath NVARCHAR(4000),
                FileSizeBytes BIGINT NOT NULL,
                CreatedDate DATETIME2 NOT NULL,
                ModifiedDate DATETIME2 NOT NULL,
                AccessedDate DATETIME2 NOT NULL,
                Content NVARCHAR(MAX),
                ContentHash NVARCHAR(100),
                IsReadable BIT NOT NULL,
                ErrorMessage NVARCHAR(1000),
                ProcessedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                INDEX IX_DeploymentReleaseFiles_RunId NONCLUSTERED (RunId),
                INDEX IX_DeploymentReleaseFiles_FullPath NONCLUSTERED (FullPath),
                INDEX IX_DeploymentReleaseFiles_FileExtension NONCLUSTERED (FileExtension),
                INDEX IX_DeploymentReleaseFiles_ModifiedDate NONCLUSTERED (ModifiedDate)
            );
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DEPLOYMENT_RELEASE' AND xtype='U')
            CREATE TABLE DEPLOYMENT_RELEASE (
                Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                RunId UNIQUEIDENTIFIER NOT NULL,
                Tags NVARCHAR(255) NOT NULL,
                Deployment NVARCHAR(255) NOT NULL,
                DeploymentDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                ProcessedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                INDEX IX_DeploymentRelease_RunId NONCLUSTERED (RunId),
                INDEX IX_DeploymentRelease_Deployment NONCLUSTERED (Deployment),
                INDEX IX_DeploymentRelease_Tags NONCLUSTERED (Tags)
            );
    """;
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(createTableSql);
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

        const string insertSql = """
            INSERT INTO DEPLOYMENT_RELEASE_FILES
            (RunId, FullPath, FileName, FileExtension, DirectoryPath, FileSizeBytes, 
             CreatedDate, ModifiedDate, AccessedDate, Content, ContentHash, 
             IsReadable, ErrorMessage)
            VALUES 
            (@RunId, @FullPath, @FileName, @FileExtension, @DirectoryPath, @FileSizeBytes, 
             @CreatedDate, @ModifiedDate, @AccessedDate, @Content, @ContentHash, 
             @IsReadable, @ErrorMessage);
            """;

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();
        try
        {
            var rowsAffected = await connection.ExecuteAsync(insertSql, fileInformationList, transaction);
            transaction.Commit();
            return rowsAffected;
        }
        catch
        {
            transaction.Rollback();
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

        await foreach (var fileInfo in fileInformationAsyncEnumerable.WithCancellation(cancellationToken))
        {
            batch.Add(fileInfo);

            if (batch.Count >= batchSize)
            {
                totalInserted += await InsertFileInformationAsync(batch, cancellationToken);
                batch.Clear();

                // Report progress
                Console.WriteLine($"Processed {totalInserted} files...");
            }
        }

        // Insert remaining items
        if (batch.Count > 0)
        {
            totalInserted += await InsertFileInformationAsync(batch, cancellationToken);
        }

        return totalInserted;
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

        const string insertSql = """
            INSERT INTO DEPLOYMENT_RELEASE
            (RunId, Tags, Deployment, DeploymentDate)
            VALUES 
            (@RunId, @Tags, @Deployment, @DeploymentDate);
            """;

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();
        try
        {
            var rowsAffected = await connection.ExecuteAsync(insertSql, deploymentRelease, transaction);
            transaction.Commit();
            return rowsAffected;
        }
        catch
        {
            transaction.Rollback();
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
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Disposes of the database service resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}