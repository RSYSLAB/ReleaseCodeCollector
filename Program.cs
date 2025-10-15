using Microsoft.Extensions.Configuration;
using ReleaseCodeCollector.Services;
using ReleaseCodeCollector.Models;
using System.Diagnostics;

// Main entry point for the Release Code Collector application.
// Collects file information from a specified directory and stores it in an MSSQL database.

try
{
    // Build configuration from appsettings.json
    var configuration = BuildConfiguration();

    // Create command line service
    var commandLineService = new CommandLineService(configuration);

    // Parse named command line arguments
    var options = commandLineService.ParseArguments(args);

    if (options.ShowHelp)
    {
        CommandLineService.PrintUsage();
        return 0;
    }

    // Validate parsed options
    var validationErrors = commandLineService.ValidateOptions(options);
    if (validationErrors.Count > 0)
    {
        foreach (var error in validationErrors)
        {
            Console.WriteLine($"Error: {error}");
        }
        Console.WriteLine();
        CommandLineService.PrintUsage();
        return 1;
    }

    // Generate unique run identifier
    var runId = Guid.NewGuid();

    // Print startup information
    Console.WriteLine("=== Release Code Collector ===");
    Console.WriteLine($"Run ID: {runId}");
    Console.WriteLine($"Source: {options.Source}");
    Console.WriteLine($"Database: {GetDatabaseFromConnectionString(options.ConnectionString)}");
    Console.WriteLine($"Batch Size: {options.BatchSize:N0}");
    Console.WriteLine($"Max File Size: {options.MaxFileSize:N0} bytes ({GetFileSize(options.MaxFileSize)})");
    Console.WriteLine($"Connection String: {options.ConnectionString}");
    Console.WriteLine();

    // Initialize services
    var databaseService = new DatabaseService(options.ConnectionString);
    var fileDiscoveryService = new FileDiscoveryService();

    // Test database connection and initialize database
    Console.WriteLine("Testing database connection...");
    await databaseService.InitializeDatabaseAsync();
    Console.WriteLine("✓ Database connection successful");
    Console.WriteLine();

    // Insert deployment release record
    Console.WriteLine("Recording deployment release information...");
    var deploymentRelease = new DeploymentRelease(
        RunId: runId,
        Tags: options.Tags,
        Deployment: options.Deployment,
        DeploymentDate: options.DeploymentDate);

    await databaseService.InsertDeploymentReleaseAsync(deploymentRelease);
    Console.WriteLine("✓ Deployment release recorded");
    Console.WriteLine();

    // Discover files
    Console.WriteLine($"Discovering files in: {options.Source}");
    var stopwatch = Stopwatch.StartNew();

    var fileCount = 0;

    // Use the async enumerable directly with the batch insert method
    var fileEnumerable = fileDiscoveryService.DiscoverFilesAsync(runId, options.Source);

    await foreach (var fileInfo in fileEnumerable)
    {
        fileCount++;
        if (options.Verbose && fileCount % 100 == 0)
        {
            Console.WriteLine($"Discovered {fileCount} files so far...");
        }
    }

    // Process all files in batches
    fileCount = await databaseService.InsertFileInformationBatchAsync(
        fileDiscoveryService.DiscoverFilesAsync(runId, options.Source),
        options.BatchSize);

    stopwatch.Stop();

    // Print completion summary
    Console.WriteLine();
    Console.WriteLine("=== Completion Summary ===");
    Console.WriteLine($"Total files processed: {fileCount:N0}");
    Console.WriteLine($"Elapsed time: {stopwatch.Elapsed:hh\\:mm\\:ss\\.fff}");
    Console.WriteLine($"Average rate: {(fileCount / stopwatch.Elapsed.TotalSeconds):F1} files/second");
    Console.WriteLine($"Run ID: {runId}");
    Console.WriteLine();
    Console.WriteLine("✓ File collection completed successfully!");

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    if (args.Contains("--verbose") || args.Contains("-v"))
    {
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
    return 1;
}

/// <summary>
/// Builds the application configuration from appsettings.json.
/// </summary>
/// <returns>The built configuration</returns>
static IConfiguration BuildConfiguration()
{
    return new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();
}

/// <summary>
/// Extracts the database name from a connection string for display purposes.
/// </summary>
/// <param name="connectionString">The connection string</param>
/// <returns>The database name or "Unknown" if not found</returns>
static string GetDatabaseFromConnectionString(string connectionString)
{
    try
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var databasePart = parts.FirstOrDefault(p => p.StartsWith("Database=", StringComparison.OrdinalIgnoreCase));
        return databasePart?.Split('=')[1] ?? "Unknown";
    }
    catch
    {
        return "Unknown";
    }
}

/// <summary>
/// Gets a human-readable file size string.
/// </summary>
/// <param name="bytes">Size in bytes</param>
/// <returns>Formatted size string</returns>
static string GetFileSize(long bytes)
{
    try
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        var order = 0;
        var size = (double)bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
    catch
    {
        return "Unknown";
    }
}