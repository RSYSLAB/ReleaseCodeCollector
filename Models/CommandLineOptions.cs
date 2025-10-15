namespace ReleaseCodeCollector.Models;

/// <summary>
/// Represents the parsed command line options.
/// </summary>
/// <param name="Source">Path to the source to scan for files</param>
/// <param name="ConnectionString">MSSQL connection string</param>
/// <param name="BatchSize">Number of files to process in each database batch</param>
/// <param name="MaxFileSize">Maximum file size in bytes to read content from</param>
/// <param name="Verbose">Enable verbose output</param>
/// <param name="ShowHelp">Show help information</param>
/// <param name="Tags">Tags associated with this run</param>
/// <param name="Deployment">Deployment identifier for this run</param>
/// <param name="DeploymentDate">Date when the deployment was created or executed</param>
public record CommandLineOptions(
    string Source,
    string ConnectionString,
    int BatchSize,
    long MaxFileSize,
    bool Verbose,
    bool ShowHelp,
    string Tags,
    string Deployment,
    DateTime DeploymentDate);