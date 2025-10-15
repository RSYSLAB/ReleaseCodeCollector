using ReleaseCodeCollector.Models;

namespace ReleaseCodeCollector.Services;

/// <summary>
/// Interface for database operations with MSSQL using Dapper.
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Creates the table structure for storing file information if it doesn't exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task InitializeDatabaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts file information records into the database using bulk operations.
    /// </summary>
    /// <param name="fileInformationList">Collection of file information to insert</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The number of records inserted</returns>
    Task<int> InsertFileInformationAsync(IEnumerable<FileInformation> fileInformationList, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts file information records in batches to handle large datasets efficiently.
    /// </summary>
    /// <param name="fileInformationAsyncEnumerable">Async enumerable of file information</param>
    /// <param name="batchSize">Number of records to process in each batch</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The total number of records inserted</returns>
    Task<int> InsertFileInformationBatchAsync(
        IAsyncEnumerable<FileInformation> fileInformationAsyncEnumerable,
        int batchSize = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a deployment release record into the database.
    /// </summary>
    /// <param name="deploymentRelease">The deployment release information to insert</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The number of records inserted (should be 1 on success)</returns>
    Task<int> InsertDeploymentReleaseAsync(DeploymentRelease deploymentRelease, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the database connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if connection is successful, false otherwise</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves deployment releases by run ID.
    /// </summary>
    /// <param name="runId">The run ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of deployment releases</returns>
    Task<IEnumerable<DeploymentRelease>> GetDeploymentReleasesByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves file information by run ID.
    /// </summary>
    /// <param name="runId">The run ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Collection of file information</returns>
    Task<IEnumerable<FileInformation>> GetFileInformationByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of files for a specific run ID.
    /// </summary>
    /// <param name="runId">The run ID to count files for</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>The count of files</returns>
    Task<int> GetFileCountByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);
}