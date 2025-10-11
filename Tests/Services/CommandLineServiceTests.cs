using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using ReleaseCodeCollector.Services;
using ReleaseCodeCollector.Models;

namespace ReleaseCodeCollector.Tests.Services;

/// <summary>
/// Simple mock configuration for testing.
/// </summary>
public class MockConfiguration : IConfiguration
{
    private readonly Dictionary<string, string> _data = new();

    public string? this[string key]
    {
        get => _data.TryGetValue(key, out var value) ? value : null;
        set => _data[key] = value ?? string.Empty;
    }

    public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();

    public IChangeToken GetReloadToken() => new MockChangeToken();

    public IConfigurationSection GetSection(string key) => new MockConfigurationSection(key, _data);
}

/// <summary>
/// Mock implementation of IConfigurationSection for testing
/// </summary>
public class MockConfigurationSection : IConfigurationSection
{
    private readonly string _key;
    private readonly Dictionary<string, string> _data;

    public MockConfigurationSection(string key, Dictionary<string, string> data)
    {
        _key = key;
        _data = data;
        Key = key.Split(':').LastOrDefault() ?? key;
        Path = key;
    }

    public string? this[string key]
    {
        get => _data.TryGetValue($"{_key}:{key}", out var value) ? value : null;
        set => _data[$"{_key}:{key}"] = value ?? string.Empty;
    }

    public string Key
    {
        get;
    }
    public string Path
    {
        get;
    }
    public string? Value
    {
        get => _data.TryGetValue(_key, out var value) ? value : null;
        set => _data[_key] = value ?? string.Empty;
    }

    public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();

    public IChangeToken GetReloadToken() => new MockChangeToken();

    public IConfigurationSection GetSection(string key) => new MockConfigurationSection($"{_key}:{key}", _data);
}

/// <summary>
/// Mock implementation of IChangeToken for testing
/// </summary>
public class MockChangeToken : IChangeToken
{
    public bool HasChanged => false;
    public bool ActiveChangeCallbacks => false;
    public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => new MockDisposable();
}

/// <summary>
/// Mock implementation of IDisposable for testing
/// </summary>
public class MockDisposable : IDisposable
{
    public void Dispose()
    {
    }
}

/// <summary>
/// Unit tests for the CommandLineService class.
/// </summary>
[TestFixture]
public class CommandLineServiceTests
{
    private IConfiguration _configuration = null!;
    private CommandLineService _service = null!;

    [SetUp]
    public void SetUp()
    {
        // Create a mock configuration with test values
        var mockConfiguration = new MockConfiguration();
        mockConfiguration["DatabaseSettings:DefaultConnectionString"] = "Server=localhost;Database=TestDB;Integrated Security=true;";
        mockConfiguration["DatabaseSettings:BatchSize"] = "500";
        mockConfiguration["FileProcessingSettings:MaxFileSizeBytes"] = "104857600";

        _configuration = mockConfiguration;

        _service = new CommandLineService(_configuration);
    }

    [Test]
    public void ParseArguments_WithValidDirectoryArgument_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "--directory", @"C:\Test" };

        // Act
        var result = _service.ParseArguments(args);

        // Assert
        Assert.That(result.Directory, Is.EqualTo(@"C:\Test"));
        Assert.That(result.ConnectionString, Is.EqualTo("Server=localhost;Database=TestDB;Integrated Security=true;"));
        Assert.That(result.BatchSize, Is.EqualTo(500));
        Assert.That(result.MaxFileSize, Is.EqualTo(104857600L));
        Assert.That(result.Verbose, Is.False);
        Assert.That(result.ShowHelp, Is.False);
    }

    [Test]
    public void ParseArguments_WithShortDirectoryArgument_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "-d", @"C:\Projects" };

        // Act
        var result = _service.ParseArguments(args);

        // Assert
        Assert.That(result.Directory, Is.EqualTo(@"C:\Projects"));
    }

    [Test]
    public void ParseArguments_WithPositionalDirectory_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { @"C:\BackwardCompatibility" };

        // Act
        var result = _service.ParseArguments(args);

        // Assert
        Assert.That(result.Directory, Is.EqualTo(@"C:\BackwardCompatibility"));
    }

    [Test]
    public void ParseArguments_WithCustomConnectionString_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "-d", @"C:\Test", "--connection", "Server=prod;Database=MyDB;" };

        // Act
        var result = _service.ParseArguments(args);

        // Assert
        Assert.That(result.ConnectionString, Is.EqualTo("Server=prod;Database=MyDB;"));
    }

    [Test]
    public void ParseArguments_WithShortConnectionArgument_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "-d", @"C:\Test", "-c", "Custom connection string" };

        // Act
        var result = _service.ParseArguments(args);

        // Assert
        Assert.That(result.ConnectionString, Is.EqualTo("Custom connection string"));
    }

    [Test]
    public void ParseArguments_WithBatchSize_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "-d", @"C:\Test", "--batch-size", "1000" };

        // Act
        var result = _service.ParseArguments(args);

        // Assert
        Assert.That(result.BatchSize, Is.EqualTo(1000));
    }

    [Test]
    public void ParseArguments_WithShortBatchSize_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "-d", @"C:\Test", "-b", "250" };

        // Act
        var result = _service.ParseArguments(args);

        // Assert
        Assert.That(result.BatchSize, Is.EqualTo(250));
    }

    [Test]
    public void ParseArguments_WithMaxFileSize_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "-d", @"C:\Test", "--max-file-size", "52428800" };

        // Act
        var result = _service.ParseArguments(args);

        // Assert
        Assert.That(result.MaxFileSize, Is.EqualTo(52428800L));
    }

    [Test]
    public void ParseArguments_WithShortMaxFileSize_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "-d", @"C:\Test", "-m", "10485760" };

        // Act
        var result = _service.ParseArguments(args);

        // Assert
        Assert.That(result.MaxFileSize, Is.EqualTo(10485760L));
    }

    [Test]
    public void ParseArguments_WithVerboseFlag_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "-d", @"C:\Test", "--verbose" };

        // Act
        var result = _service.ParseArguments(args);

        // Assert
        Assert.That(result.Verbose, Is.True);
    }

    [Test]
    public void ParseArguments_WithShortVerboseFlag_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "-d", @"C:\Test", "-v" };

        // Act
        var result = _service.ParseArguments(args);

        // Assert
        Assert.That(result.Verbose, Is.True);
    }

    [Test]
    public void ParseArguments_WithHelpFlag_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var result = _service.ParseArguments(args);

        // Assert
        Assert.That(result.ShowHelp, Is.True);
    }

    [Test]
    [TestCase("-h")]
    [TestCase("/?")]
    public void ParseArguments_WithDifferentHelpFlags_ParsesCorrectly(string helpFlag)
    {
        // Arrange
        var args = new[] { helpFlag };

        // Act
        var result = _service.ParseArguments(args);

        // Assert
        Assert.That(result.ShowHelp, Is.True);
    }

    [Test]
    public void ParseArguments_WithAllArguments_ParsesCorrectly()
    {
        // Arrange
        var args = new[] {
            "--directory", @"C:\FullTest",
            "--connection", "Server=full;Database=Test;",
            "--batch-size", "750",
            "--max-file-size", "209715200",
            "--verbose"
        };

        // Act
        var result = _service.ParseArguments(args);

        // Assert
        Assert.That(result.Directory, Is.EqualTo(@"C:\FullTest"));
        Assert.That(result.ConnectionString, Is.EqualTo("Server=full;Database=Test;"));
        Assert.That(result.BatchSize, Is.EqualTo(750));
        Assert.That(result.MaxFileSize, Is.EqualTo(209715200L));
        Assert.That(result.Verbose, Is.True);
        Assert.That(result.ShowHelp, Is.False);
    }

    [Test]
    public void ParseArguments_WithEnvironmentVariables_ExpandsCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TEST_PATH", @"C:\TestExpansion");
        Environment.SetEnvironmentVariable("TEST_SERVER", "testserver");

        try
        {
            var args = new[] { "--directory", "%TEST_PATH%", "--connection", "Server=%TEST_SERVER%;Database=Test;" };

            // Act
            var result = _service.ParseArguments(args);

            // Assert
            Assert.That(result.Directory, Is.EqualTo(@"C:\TestExpansion"));
            Assert.That(result.ConnectionString, Is.EqualTo("Server=testserver;Database=Test;"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST_PATH", null);
            Environment.SetEnvironmentVariable("TEST_SERVER", null);
        }
    }

    [Test]
    public void ParseArguments_WithUnixStyleEnvironmentVariables_ExpandsCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("UNIX_TEST_PATH", @"C:\UnixTest");

        try
        {
            var args = new[] { "--directory", "$UNIX_TEST_PATH" };

            // Act
            var result = _service.ParseArguments(args);

            // Assert
            Assert.That(result.Directory, Is.EqualTo(@"C:\UnixTest"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("UNIX_TEST_PATH", null);
        }
    }

    [Test]
    public void ParseArguments_WithBracedEnvironmentVariables_ExpandsCorrectly()
    {
        // Arrange
        Environment.SetEnvironmentVariable("BRACED_TEST_PATH", @"C:\BracedTest");

        try
        {
            var args = new[] { "--directory", "${BRACED_TEST_PATH}" };

            // Act
            var result = _service.ParseArguments(args);

            // Assert
            Assert.That(result.Directory, Is.EqualTo(@"C:\BracedTest"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("BRACED_TEST_PATH", null);
        }
    }

    [Test]
    public void ParseArguments_WithMissingDirectoryValue_ThrowsException()
    {
        // Arrange
        var args = new[] { "--directory" };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _service.ParseArguments(args));
        Assert.That(ex?.Message, Is.EqualTo("Directory parameter requires a value."));
    }

    [Test]
    public void ParseArguments_WithMissingConnectionValue_ThrowsException()
    {
        // Arrange
        var args = new[] { "-d", @"C:\Test", "--connection" };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _service.ParseArguments(args));
        Assert.That(ex?.Message, Is.EqualTo("Connection string parameter requires a value."));
    }

    [Test]
    public void ParseArguments_WithInvalidBatchSize_ThrowsException()
    {
        // Arrange
        var args = new[] { "-d", @"C:\Test", "--batch-size", "invalid" };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _service.ParseArguments(args));
        Assert.That(ex?.Message, Is.EqualTo("Batch size must be a valid integer."));
    }

    [Test]
    public void ParseArguments_WithMissingBatchSizeValue_ThrowsException()
    {
        // Arrange
        var args = new[] { "-d", @"C:\Test", "--batch-size" };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _service.ParseArguments(args));
        Assert.That(ex?.Message, Is.EqualTo("Batch size parameter requires a value."));
    }

    [Test]
    public void ParseArguments_WithInvalidMaxFileSize_ThrowsException()
    {
        // Arrange
        var args = new[] { "-d", @"C:\Test", "--max-file-size", "notanumber" };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _service.ParseArguments(args));
        Assert.That(ex?.Message, Is.EqualTo("Max file size must be a valid number."));
    }

    [Test]
    public void ParseArguments_WithUnknownParameter_ThrowsException()
    {
        // Arrange
        var args = new[] { "-d", @"C:\Test", "--unknown-param", "value" };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => _service.ParseArguments(args));
        Assert.That(ex?.Message, Is.EqualTo("Unknown parameter: --unknown-param"));
    }

    [Test]
    public void ValidateOptions_WithValidOptions_ReturnsNoErrors()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var options = new CommandLineOptions(
            tempDir,
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            false,
            false);

        // Act
        var errors = _service.ValidateOptions(options);

        // Assert
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void ValidateOptions_WithEmptyDirectory_ReturnsError()
    {
        // Arrange
        var options = new CommandLineOptions(
            "",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            false,
            false);

        // Act
        var errors = _service.ValidateOptions(options);

        // Assert
        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0], Is.EqualTo("Directory parameter is required."));
    }

    [Test]
    public void ValidateOptions_WithNonExistentDirectory_ReturnsError()
    {
        // Arrange
        var options = new CommandLineOptions(
            @"C:\NonExistentDirectory\SubDir",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            false,
            false);

        // Act
        var errors = _service.ValidateOptions(options);

        // Assert
        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0], Is.EqualTo(@"Directory 'C:\NonExistentDirectory\SubDir' does not exist."));
    }

    [Test]
    public void ValidateOptions_WithZeroBatchSize_ReturnsError()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var options = new CommandLineOptions(
            tempDir,
            "Server=localhost;Database=Test;",
            0,
            1048576L,
            false,
            false);

        // Act
        var errors = _service.ValidateOptions(options);

        // Assert
        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0], Is.EqualTo("Batch size must be greater than 0."));
    }

    [Test]
    public void ValidateOptions_WithNegativeBatchSize_ReturnsError()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var options = new CommandLineOptions(
            tempDir,
            "Server=localhost;Database=Test;",
            -100,
            1048576L,
            false,
            false);

        // Act
        var errors = _service.ValidateOptions(options);

        // Assert
        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0], Is.EqualTo("Batch size must be greater than 0."));
    }

    [Test]
    public void ValidateOptions_WithZeroMaxFileSize_ReturnsError()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var options = new CommandLineOptions(
            tempDir,
            "Server=localhost;Database=Test;",
            500,
            0L,
            false,
            false);

        // Act
        var errors = _service.ValidateOptions(options);

        // Assert
        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0], Is.EqualTo("Max file size must be greater than 0."));
    }

    [Test]
    public void ValidateOptions_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var options = new CommandLineOptions(
            "",
            "Server=localhost;Database=Test;",
            -1,
            -1L,
            false,
            false);

        // Act
        var errors = _service.ValidateOptions(options);

        // Assert
        Assert.That(errors, Has.Count.EqualTo(3));
        Assert.That(errors, Does.Contain("Directory parameter is required."));
        Assert.That(errors, Does.Contain("Batch size must be greater than 0."));
        Assert.That(errors, Does.Contain("Max file size must be greater than 0."));
    }

    [Test]
    public void PrintUsage_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => CommandLineService.PrintUsage());
    }

    [Test]
    public void Constructor_WithNullConfiguration_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CommandLineService(null!));
    }
}