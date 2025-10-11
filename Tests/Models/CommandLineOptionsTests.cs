using NUnit.Framework;
using ReleaseCodeCollector.Models;

namespace ReleaseCodeCollector.Tests.Models;

/// <summary>
/// Unit tests for the CommandLineOptions record.
/// </summary>
[TestFixture]
public class CommandLineOptionsTests
{
    [Test]
    public void CommandLineOptions_Constructor_SetsAllProperties()
    {
        // Arrange
        var directory = @"C:\Test";
        var connectionString = "Server=localhost;Database=Test;";
        var batchSize = 1000;
        var maxFileSize = 5242880L; // 5MB
        var verbose = true;
        var showHelp = false;

        // Act
        var options = new CommandLineOptions(
            directory,
            connectionString,
            batchSize,
            maxFileSize,
            verbose,
            showHelp);

        // Assert
        Assert.That(options.Directory, Is.EqualTo(directory));
        Assert.That(options.ConnectionString, Is.EqualTo(connectionString));
        Assert.That(options.BatchSize, Is.EqualTo(batchSize));
        Assert.That(options.MaxFileSize, Is.EqualTo(maxFileSize));
        Assert.That(options.Verbose, Is.EqualTo(verbose));
        Assert.That(options.ShowHelp, Is.EqualTo(showHelp));
    }

    [Test]
    public void CommandLineOptions_Equality_WorksCorrectly()
    {
        // Arrange
        var options1 = new CommandLineOptions(
            @"C:\Test",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            true,
            false);

        var options2 = new CommandLineOptions(
            @"C:\Test",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            true,
            false);

        // Act & Assert
        Assert.That(options1, Is.EqualTo(options2));
        Assert.That(options1.GetHashCode(), Is.EqualTo(options2.GetHashCode()));
    }

    [Test]
    public void CommandLineOptions_Inequality_WorksCorrectly()
    {
        // Arrange
        var options1 = new CommandLineOptions(
            @"C:\Test1",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            true,
            false);

        var options2 = new CommandLineOptions(
            @"C:\Test2",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            true,
            false);

        // Act & Assert
        Assert.That(options1, Is.Not.EqualTo(options2));
    }

    [Test]
    public void CommandLineOptions_WithDifferentBatchSizes_AreNotEqual()
    {
        // Arrange
        var options1 = new CommandLineOptions(
            @"C:\Test",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            false,
            false);

        var options2 = new CommandLineOptions(
            @"C:\Test",
            "Server=localhost;Database=Test;",
            1000,
            1048576L,
            false,
            false);

        // Act & Assert
        Assert.That(options1, Is.Not.EqualTo(options2));
    }

    [Test]
    public void CommandLineOptions_WithDifferentVerboseSettings_AreNotEqual()
    {
        // Arrange
        var options1 = new CommandLineOptions(
            @"C:\Test",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            true,
            false);

        var options2 = new CommandLineOptions(
            @"C:\Test",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            false,
            false);

        // Act & Assert
        Assert.That(options1, Is.Not.EqualTo(options2));
    }

    [Test]
    public void CommandLineOptions_ToString_ContainsKeyInformation()
    {
        // Arrange
        var options = new CommandLineOptions(
            @"C:\MyProject",
            "Server=prod;Database=CodeDB;",
            750,
            2097152L,
            true,
            false);

        // Act
        var result = options.ToString();

        // Assert
        Assert.That(result, Does.Contain(@"C:\MyProject"));
        Assert.That(result, Does.Contain("750"));
        Assert.That(result, Does.Contain("True"));
    }

    [Test]
    [TestCase("", "Valid connection", 500, 1024L, false, false)]
    [TestCase(null, "Valid connection", 500, 1024L, false, false)]
    [TestCase(@"C:\Test", "", 500, 1024L, false, false)]
    [TestCase(@"C:\Test", null, 500, 1024L, false, false)]
    public void CommandLineOptions_HandlesNullOrEmptyStrings(
        string directory,
        string connectionString,
        int batchSize,
        long maxFileSize,
        bool verbose,
        bool showHelp)
    {
        // Act & Assert - Should not throw exceptions
        Assert.DoesNotThrow(() =>
        {
            var options = new CommandLineOptions(
                directory,
                connectionString,
                batchSize,
                maxFileSize,
                verbose,
                showHelp);

            Assert.That(options.Directory, Is.EqualTo(directory));
            Assert.That(options.ConnectionString, Is.EqualTo(connectionString));
        });
    }

    [Test]
    public void CommandLineOptions_WithMaxValues_HandlesCorrectly()
    {
        // Arrange & Act
        var options = new CommandLineOptions(
            @"C:\VeryLongDirectoryPathThatExceedsNormalLength\SubDirectory\AnotherSubDirectory",
            "Server=very-long-server-name.domain.com;Database=VeryLongDatabaseNameThatExceedsNormalLength;Integrated Security=true;",
            int.MaxValue,
            long.MaxValue,
            true,
            true);

        // Assert
        Assert.That(options.BatchSize, Is.EqualTo(int.MaxValue));
        Assert.That(options.MaxFileSize, Is.EqualTo(long.MaxValue));
        Assert.That(options.Verbose, Is.True);
        Assert.That(options.ShowHelp, Is.True);
    }
}