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
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ReleaseCodeFiles' AND xtype='U')
            CREATE TABLE ReleaseCodeFiles (
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
                INDEX IX_ReleaseCodeFiles_RunId NONCLUSTERED (RunId),
                INDEX IX_ReleaseCodeFiles_FullPath NONCLUSTERED (FullPath),
                INDEX IX_ReleaseCodeFiles_FileExtension NONCLUSTERED (FileExtension),
                INDEX IX_ReleaseCodeFiles_ModifiedDate NONCLUSTERED (ModifiedDate)
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
            INSERT INTO ReleaseCodeFiles 
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