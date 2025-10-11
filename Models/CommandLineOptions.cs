namespace ReleaseCodeCollector.Models;

/// <summary>
/// Represents the parsed command line options.
/// </summary>
/// <param name="Directory">Path to the directory to scan for files</param>
/// <param name="ConnectionString">MSSQL connection string</param>
/// <param name="BatchSize">Number of files to process in each database batch</param>
/// <param name="MaxFileSize">Maximum file size in bytes to read content from</param>
/// <param name="Verbose">Enable verbose output</param>
/// <param name="ShowHelp">Show help information</param>
public record CommandLineOptions(
    string Directory,
    string ConnectionString,
    int BatchSize,
    long MaxFileSize,
    bool Verbose,
    bool ShowHelp);