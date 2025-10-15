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
        var source = @"C:\Test";
        var connectionString = "Server=localhost;Database=Test;";
        var batchSize = 1000;
        var maxFileSize = 5242880L; // 5MB
        var verbose = true;
        var showHelp = false;
        var tags = "test,unit";
        var deployment = "test-deployment";
        var deploymentDate = new DateTime(2025, 10, 15, 14, 30, 0);

        // Act
        var options = new CommandLineOptions(
            source,
            connectionString,
            batchSize,
            maxFileSize,
            verbose,
            showHelp,
            tags,
            deployment,
            deploymentDate);

        // Assert
        Assert.That(options.Source, Is.EqualTo(source));
        Assert.That(options.ConnectionString, Is.EqualTo(connectionString));
        Assert.That(options.BatchSize, Is.EqualTo(batchSize));
        Assert.That(options.MaxFileSize, Is.EqualTo(maxFileSize));
        Assert.That(options.Verbose, Is.EqualTo(verbose));
        Assert.That(options.ShowHelp, Is.EqualTo(showHelp));
        Assert.That(options.Tags, Is.EqualTo(tags));
        Assert.That(options.Deployment, Is.EqualTo(deployment));
        Assert.That(options.DeploymentDate, Is.EqualTo(deploymentDate));
    }

    [Test]
    public void CommandLineOptions_Equality_WorksCorrectly()
    {
        // Arrange
        var deploymentDate = new DateTime(2025, 10, 15, 14, 30, 0);
        var options1 = new CommandLineOptions(
            @"C:\Test",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            true,
            false,
            "test,unit",
            "test-deployment",
            deploymentDate);

        var options2 = new CommandLineOptions(
            @"C:\Test",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            true,
            false,
            "test,unit",
            "test-deployment",
            deploymentDate);

        // Act & Assert
        Assert.That(options1, Is.EqualTo(options2));
        Assert.That(options1.GetHashCode(), Is.EqualTo(options2.GetHashCode()));
    }

    [Test]
    public void CommandLineOptions_Inequality_WorksCorrectly()
    {
        // Arrange
        var deploymentDate = new DateTime(2025, 10, 15, 14, 30, 0);
        var options1 = new CommandLineOptions(
            @"C:\Test1",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            true,
            false,
            "test,unit",
            "test-deployment",
            deploymentDate);

        var options2 = new CommandLineOptions(
            @"C:\Test2",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            true,
            false,
            "test,unit",
            "test-deployment",
            deploymentDate);

        // Act & Assert
        Assert.That(options1, Is.Not.EqualTo(options2));
    }

    [Test]
    public void CommandLineOptions_WithDifferentBatchSizes_AreNotEqual()
    {
        // Arrange
        var deploymentDate = new DateTime(2025, 10, 15, 14, 30, 0);
        var options1 = new CommandLineOptions(
            @"C:\Test",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            false,
            false,
            "test,unit",
            "test-deployment",
            deploymentDate);

        var options2 = new CommandLineOptions(
            @"C:\Test",
            "Server=localhost;Database=Test;",
            1000,
            1048576L,
            false,
            false,
            "test,unit",
            "test-deployment",
            deploymentDate);

        // Act & Assert
        Assert.That(options1, Is.Not.EqualTo(options2));
    }

    [Test]
    public void CommandLineOptions_WithDifferentVerboseSettings_AreNotEqual()
    {
        // Arrange
        var deploymentDate = new DateTime(2025, 10, 15, 14, 30, 0);
        var options1 = new CommandLineOptions(
            @"C:\Test",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            true,
            false,
            "test,unit",
            "test-deployment",
            deploymentDate);

        var options2 = new CommandLineOptions(
            @"C:\Test",
            "Server=localhost;Database=Test;",
            500,
            1048576L,
            false,
            false,
            "test,unit",
            "test-deployment",
            deploymentDate);

        // Act & Assert
        Assert.That(options1, Is.Not.EqualTo(options2));
    }

    [Test]
    public void CommandLineOptions_ToString_ContainsKeyInformation()
    {
        // Arrange
        var deploymentDate = new DateTime(2025, 10, 15, 14, 30, 0);
        var options = new CommandLineOptions(
            @"C:\MyProject",
            "Server=prod;Database=CodeDB;",
            750,
            2097152L,
            true,
            false,
            "v1.0,production",
            "release-deployment",
            deploymentDate);

        // Act
        var result = options.ToString();

        // Assert
        Assert.That(result, Does.Contain(@"C:\MyProject"));
        Assert.That(result, Does.Contain("750"));
        Assert.That(result, Does.Contain("True"));
        Assert.That(result, Does.Contain("v1.0,production"));
        Assert.That(result, Does.Contain("release-deployment"));
    }

    [Test]
    [TestCase("", "Valid connection", 500, 1024L, false, false)]
    [TestCase(null, "Valid connection", 500, 1024L, false, false)]
    [TestCase(@"C:\Test", "", 500, 1024L, false, false)]
    [TestCase(@"C:\Test", null, 500, 1024L, false, false)]
    public void CommandLineOptions_HandlesNullOrEmptyStrings(
        string source,
        string connectionString,
        int batchSize,
        long maxFileSize,
        bool verbose,
        bool showHelp)
    {
        // Arrange
        var deploymentDate = new DateTime(2025, 10, 15, 14, 30, 0);

        // Act & Assert - Should not throw exceptions
        Assert.DoesNotThrow(() =>
        {
            var options = new CommandLineOptions(
                source,
                connectionString,
                batchSize,
                maxFileSize,
                verbose,
                showHelp,
                "test,unit",
                "test-deployment",
                deploymentDate);

            Assert.That(options.Source, Is.EqualTo(source));
            Assert.That(options.ConnectionString, Is.EqualTo(connectionString));
        });
    }

    [Test]
    public void CommandLineOptions_WithMaxValues_HandlesCorrectly()
    {
        // Arrange & Act
        var deploymentDate = new DateTime(2025, 10, 15, 14, 30, 0);
        var options = new CommandLineOptions(
            @"C:\VeryLongDirectoryPathThatExceedsNormalLength\SubDirectory\AnotherSubDirectory",
            "Server=very-long-server-name.domain.com;Database=VeryLongDatabaseNameThatExceedsNormalLength;Integrated Security=true;",
            int.MaxValue,
            long.MaxValue,
            true,
            true,
            "v1.0,production,test,unit,integration",
            "very-long-deployment-name-that-exceeds-normal-length",
            deploymentDate);

        // Assert
        Assert.That(options.BatchSize, Is.EqualTo(int.MaxValue));
        Assert.That(options.MaxFileSize, Is.EqualTo(long.MaxValue));
        Assert.That(options.Verbose, Is.True);
        Assert.That(options.ShowHelp, Is.True);
        Assert.That(options.Tags, Is.EqualTo("v1.0,production,test,unit,integration"));
        Assert.That(options.Deployment, Is.EqualTo("very-long-deployment-name-that-exceeds-normal-length"));
        Assert.That(options.DeploymentDate, Is.EqualTo(deploymentDate));
    }
}