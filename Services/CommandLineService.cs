using Microsoft.Extensions.Configuration;
using ReleaseCodeCollector.Models;
using System.Text.RegularExpressions;

namespace ReleaseCodeCollector.Services;

/// <summary>
/// Service responsible for parsing and processing command line arguments.
/// </summary>
public class CommandLineService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the CommandLineService.
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    public CommandLineService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Parses named command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Parsed options</returns>
    public CommandLineOptions ParseArguments(string[] args)
    {
        var directory = string.Empty;
        var connectionString = _configuration["DatabaseSettings:DefaultConnectionString"]
            ?? "Server=localhost;Database=ReleaseCodeCollector;Integrated Security=true;TrustServerCertificate=true;";
        var batchSize = _configuration.GetValue<int>("DatabaseSettings:BatchSize", 500);
        var maxFileSize = _configuration.GetValue<long>("FileProcessingSettings:MaxFileSizeBytes", 100L * 1024 * 1024);
        var verbose = false;
        var tags = string.Empty;
        var deployment = string.Empty;
        var deploymentDate = DateTime.UtcNow;
        var showHelp = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLowerInvariant();

            switch (arg)
            {
                case "--source" or "-s":
                    if (i + 1 < args.Length)
                    {
                        directory = ExpandEnvironmentVariables(args[++i]);
                    }
                    else
                    {
                        throw new ArgumentException("Source parameter requires a value.");
                    }
                    break;

                case "--connection" or "-c":
                    if (i + 1 < args.Length)
                    {
                        connectionString = ExpandEnvironmentVariables(args[++i]);
                    }
                    else
                    {
                        throw new ArgumentException("Connection string parameter requires a value.");
                    }
                    break;
                case "--tags" or "-t":
                    if (i + 1 < args.Length)
                    {
                        tags = ExpandEnvironmentVariables(args[++i]);
                    }
                    else
                    {
                        throw new ArgumentException("Tags parameter requires a value.");
                    }
                    break;
                case "--deployment" or "-d":
                    if (i + 1 < args.Length)
                    {
                        deployment = ExpandEnvironmentVariables(args[++i]);
                    }
                    else
                    {
                        throw new ArgumentException("Deployment parameter requires a value.");
                    }
                    break;
                case "--deployment-date" or "-dd":
                    if (i + 1 < args.Length)
                    {
                        if (!DateTime.TryParse(args[++i], out deploymentDate))
                        {
                            throw new ArgumentException("Deployment date must be a valid date format.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Deployment date parameter requires a value.");
                    }
                    break;
                case "--batch-size" or "-b":
                    if (i + 1 < args.Length)
                    {
                        if (!int.TryParse(args[++i], out batchSize))
                        {
                            throw new ArgumentException("Batch size must be a valid integer.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Batch size parameter requires a value.");
                    }
                    break;

                case "--max-file-size" or "-m":
                    if (i + 1 < args.Length)
                    {
                        if (!long.TryParse(args[++i], out maxFileSize))
                        {
                            throw new ArgumentException("Max file size must be a valid number.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Max file size parameter requires a value.");
                    }
                    break;

                case "--verbose" or "-v":
                    verbose = true;
                    break;

                case "--help" or "-h" or "/?":
                    showHelp = true;
                    break;

                default:
                    // If no flag is provided and it's not a value for a previous flag, treat as directory for backward compatibility
                    if (!arg.StartsWith("-") && string.IsNullOrEmpty(directory))
                    {
                        directory = ExpandEnvironmentVariables(args[i]);
                    }
                    else if (arg.StartsWith("-"))
                    {
                        throw new ArgumentException($"Unknown parameter: {args[i]}");
                    }
                    break;
            }
        }

        return new CommandLineOptions(directory, connectionString, batchSize, maxFileSize, verbose, showHelp, tags, deployment, deploymentDate);
    }

    /// <summary>
    /// Validates the parsed command line options.
    /// </summary>
    /// <param name="options">Options to validate</param>
    /// <returns>List of validation errors, empty if valid</returns>
    public List<string> ValidateOptions(CommandLineOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(options.Source))
        {
            errors.Add("Source parameter is required.");
        }
        else if (!Directory.Exists(options.Source))
        {
            errors.Add($"Source '{options.Source}' does not exist.");
        }

        if (options.BatchSize <= 0)
        {
            errors.Add("Batch size must be greater than 0.");
        }

        if (options.MaxFileSize <= 0)
        {
            errors.Add("Max file size must be greater than 0.");
        }

        return errors;
    }

    /// <summary>
    /// Prints enhanced usage information for the application with named arguments.
    /// </summary>
    public static void PrintUsage()
    {
        Console.WriteLine("Release Code Collector");
        Console.WriteLine("======================");
        Console.WriteLine("Scans directories and stores file information in MSSQL database");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  ReleaseCodeCollector --source <path> [OPTIONS]");
        Console.WriteLine("  ReleaseCodeCollector <path> [OPTIONS]           (backward compatibility)");
        Console.WriteLine();
        Console.WriteLine("REQUIRED:");
        Console.WriteLine("  -s, --source <path>           Path to the source directory to scan for files");
        Console.WriteLine("                                Supports environment variables: %VAR%, $VAR, ${VAR}");
        Console.WriteLine("  -t, --tags <string>           Comma-separated list of tags to associate with this run");
        Console.WriteLine("  -d, --deployment <string>     Deployment identifier for this run");
        Console.WriteLine("  -dd, --deployment-date <date> Date when the deployment was created or executed");
        Console.WriteLine("                                Default: Current UTC date and time");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  -c, --connection <string>     MSSQL connection string");
        Console.WriteLine("                                Supports environment variables: %VAR%, $VAR, ${VAR}");
        Console.WriteLine("                                Default: Server=localhost;Database=ReleaseCodeCollector;Integrated Security=true;TrustServerCertificate=true;");
        Console.WriteLine("  -b, --batch-size <number>     Number of files to process in each database batch");
        Console.WriteLine("                                Default: 500");
        Console.WriteLine("  -m, --max-file-size <bytes>   Maximum file size in bytes to read content from");
        Console.WriteLine("                                Default: 104857600 (100 MB)");
        Console.WriteLine("  -v, --verbose                 Enable verbose output");
        Console.WriteLine("  -h, --help                    Show this help information");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  # Basic usage - scan C:\\Projects with default settings");
        Console.WriteLine("  ReleaseCodeCollector --source C:\\Projects");
        Console.WriteLine("  ReleaseCodeCollector C:\\Projects");
        Console.WriteLine();
        Console.WriteLine("  # Using environment variables");
        Console.WriteLine("  ReleaseCodeCollector --source %MY_PROJECT_PATH%");
        Console.WriteLine("  ReleaseCodeCollector -s $HOME/projects -c \"Server=%DB_SERVER%;Database=%DB_NAME%;\"");
        Console.WriteLine();
        Console.WriteLine("  # With custom connection string");
        Console.WriteLine("  ReleaseCodeCollector -s C:\\Code -c \"Server=myserver;Database=MyDB;User Id=user;Password=pass;\"");
        Console.WriteLine();
        Console.WriteLine("  # With custom batch size and verbose output");
        Console.WriteLine("  ReleaseCodeCollector --source D:\\Source --batch-size 1000 --verbose");
        Console.WriteLine();
        Console.WriteLine("  # With deployment tags and deployment date");
        Console.WriteLine("  ReleaseCodeCollector -s C:\\Projects -t \"v1.0,production\" -d \"Release-2025-10-15\" -dd \"2025-10-15 14:30:00\"");
        Console.WriteLine();
        Console.WriteLine("  # Full configuration");
        Console.WriteLine("  ReleaseCodeCollector -s C:\\Projects -c \"Server=prod;Database=CodeDB;Integrated Security=true;\" -b 250 -m 52428800 -v");
    }

    /// <summary>
    /// Expands environment variables in a string value.
    /// Supports Windows (%VAR%), Unix ($VAR), and braced (${VAR}) formats.
    /// </summary>
    /// <param name="value">String that may contain environment variables</param>
    /// <returns>String with environment variables expanded</returns>
    private static string ExpandEnvironmentVariables(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // First handle Windows-style %VAR% using built-in method
        value = Environment.ExpandEnvironmentVariables(value);

        // Then handle Unix-style $VAR and ${VAR} patterns
        // Match $VAR or ${VAR} patterns
        var pattern = @"\$(?:\{([^}]+)\}|([A-Za-z_][A-Za-z0-9_]*))";

        return Regex.Replace(value, pattern, match =>
        {
            // Get variable name from either ${VAR} (group 1) or $VAR (group 2) format
            var varName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            var envValue = Environment.GetEnvironmentVariable(varName);

            // Return the environment variable value if found, otherwise leave the original text
            return envValue ?? match.Value;
        });
    }
}