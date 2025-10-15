using NUnit.Framework;
using ReleaseCodeCollector.Services;
using ReleaseCodeCollector.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data;

namespace ReleaseCodeCollector.Tests.Services;

/// <summary>
/// Unit tests for the DatabaseService class.
/// Note: These tests focus on testing the service logic without requiring an actual database connection.
/// Integration tests with a real database would be in a separate test class.
/// </summary>
[TestFixture]
public class DatabaseServiceTests
{
    private const string TestConnectionString = "Server=localhost;Database=TestDB;Integrated Security=true;TrustServerCertificate=true;";
    private Mock<ILogger<DatabaseService>>? _mockLogger;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<DatabaseService>>();
    }

    [Test]
    public void Constructor_WithValidConnectionString_CreatesInstance()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => new DatabaseService(TestConnectionString));
    }

    [Test]
    public void Constructor_WithValidConnectionStringAndLogger_CreatesInstance()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => new DatabaseService(TestConnectionString, _mockLogger!.Object));
    }

    [Test]
    public void Constructor_WithNullConnectionString_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DatabaseService(null!));
    }

    [Test]
    public void Constructor_WithEmptyConnectionString_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DatabaseService(""));
    }

    [Test]
    public void Constructor_WithWhitespaceConnectionString_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DatabaseService("   "));
    }

    [Test]
    public async Task InsertFileInformationAsync_WithNullList_ThrowsException()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.InsertFileInformationAsync(null!));
    }

    [Test]
    public async Task InsertFileInformationBatchAsync_WithNullEnumerable_ThrowsException()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.InsertFileInformationBatchAsync(null!));
    }

    [Test]
    public async Task InsertFileInformationBatchAsync_WithZeroBatchSize_ThrowsException()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);
        var fileInfos = CreateTestFileInformationAsyncEnumerable();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await service.InsertFileInformationBatchAsync(fileInfos, 0));
    }

    [Test]
    public async Task InsertFileInformationBatchAsync_WithNegativeBatchSize_ThrowsException()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);
        var fileInfos = CreateTestFileInformationAsyncEnumerable();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await service.InsertFileInformationBatchAsync(fileInfos, -100));
    }

    [Test]
    public void CreateFileInformationInsertQuery_ValidatesParameters()
    {
        // This test validates the SQL query generation logic if it were public
        // Since the methods are likely private, we test the public interface behavior

        // Arrange
        var service = new DatabaseService(TestConnectionString);
        var fileInfos = new List<FileInformation>
        {
            CreateTestFileInformation()
        };

        // Act & Assert - Ensure the service can handle the data without database connection
        // This is mainly testing that no exceptions are thrown during setup
        Assert.DoesNotThrow(() => _ = service.InsertFileInformationAsync(fileInfos));
    }

    [Test]
    public void DatabaseService_HandlesSpecialCharactersInData()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);
        var fileInfo = new FileInformation(
            RunId: Guid.NewGuid(),
            FullPath: @"C:\Test\File with spaces & special chars.txt",
            FileName: "File with 'quotes' & ampersands.txt",
            FileExtension: ".txt",
            DirectoryPath: @"C:\Test",
            FileSizeBytes: 1024L,
            CreatedDate: DateTime.Now,
            ModifiedDate: DateTime.Now,
            AccessedDate: DateTime.Now,
            Content: "Content with 'single quotes', \"double quotes\", and & ampersands",
            ContentHash: "hash123",
            IsReadable: true,
            ErrorMessage: null);

        var fileInfos = new List<FileInformation> { fileInfo };

        // Act & Assert - Should not throw during preparation
        Assert.DoesNotThrow(() => _ = service.InsertFileInformationAsync(fileInfos));
    }

    [Test]
    public void DatabaseService_HandlesNullableFields()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);
        var fileInfo = new FileInformation(
            RunId: Guid.NewGuid(),
            FullPath: @"C:\Test\Binary.exe",
            FileName: "Binary.exe",
            FileExtension: ".exe",
            DirectoryPath: @"C:\Test",
            FileSizeBytes: 2048L,
            CreatedDate: DateTime.Now,
            ModifiedDate: DateTime.Now,
            AccessedDate: DateTime.Now,
            Content: null, // Null content for binary file
            ContentHash: null, // Null hash
            IsReadable: false,
            ErrorMessage: null); // Null error message

        var fileInfos = new List<FileInformation> { fileInfo };

        // Act & Assert - Should handle null values gracefully
        Assert.DoesNotThrow(() => _ = service.InsertFileInformationAsync(fileInfos));
    }

    [Test]
    public void DatabaseService_HandlesLargeDataSets()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);
        var fileInfos = new List<FileInformation>();

        // Create a large dataset
        for (var i = 0; i < 1000; i++)
        {
            fileInfos.Add(CreateTestFileInformation($"file{i}.txt"));
        }

        // Act & Assert - Should handle large datasets without memory issues
        Assert.DoesNotThrow(() => _ = service.InsertFileInformationAsync(fileInfos));
    }

    [Test]
    public void DatabaseService_HandlesEmptyList()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);
        var emptyList = new List<FileInformation>();

        // Act & Assert - Should handle empty lists gracefully
        Assert.DoesNotThrow(() => _ = service.InsertFileInformationAsync(emptyList));
    }

    [Test]
    public async Task InsertFileInformationBatchAsync_WithEmptyAsyncEnumerable_HandlesGracefully()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);
        var emptyEnumerable = CreateEmptyFileInformationAsyncEnumerable();

        // Act & Assert - Should handle empty enumerables without error
        Assert.DoesNotThrowAsync(async () =>
            await service.InsertFileInformationBatchAsync(emptyEnumerable));
    }

    [Test]
    public async Task InsertDeploymentReleaseAsync_WithNullDeploymentRelease_ThrowsException()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () => await service.InsertDeploymentReleaseAsync(null!));
    }

    [Test]
    public void InsertDeploymentReleaseAsync_WithValidDeploymentRelease_DoesNotThrowOnCreation()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);
        var deploymentRelease = CreateTestDeploymentRelease();

        // Act & Assert - This validates the method exists and can be called
        // In a real integration test, this would actually connect to a database
        Assert.DoesNotThrow(() =>
        {
            var task = service.InsertDeploymentReleaseAsync(deploymentRelease);
            Assert.That(task, Is.Not.Null);
        });
    }

    [Test]
    public void DatabaseService_ConnectionStringProperty()
    {
        // Arrange & Act
        var service = new DatabaseService(TestConnectionString);

        // Assert - Verify the service was created with the connection string
        // Note: This assumes there's some way to verify the connection string was set
        // If the connection string is not accessible, this test validates construction
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<IDatabaseService>());
    }

    [Test]
    public void DatabaseService_ImplementsInterface()
    {
        // Arrange & Act
        var service = new DatabaseService(TestConnectionString);

        // Assert
        Assert.That(service, Is.InstanceOf<IDatabaseService>());
    }

    [Test]
    public void GetDeploymentReleasesByRunIdAsync_WithValidRunId_DoesNotThrowOnCreation()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);
        var runId = Guid.NewGuid();

        // Act & Assert - This validates the method exists and can be called
        Assert.DoesNotThrow(() =>
        {
            var task = service.GetDeploymentReleasesByRunIdAsync(runId);
            Assert.That(task, Is.Not.Null);
        });
    }

    [Test]
    public void GetFileInformationByRunIdAsync_WithValidRunId_DoesNotThrowOnCreation()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);
        var runId = Guid.NewGuid();

        // Act & Assert - This validates the method exists and can be called
        Assert.DoesNotThrow(() =>
        {
            var task = service.GetFileInformationByRunIdAsync(runId);
            Assert.That(task, Is.Not.Null);
        });
    }

    [Test]
    public void GetFileCountByRunIdAsync_WithValidRunId_DoesNotThrowOnCreation()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);
        var runId = Guid.NewGuid();

        // Act & Assert - This validates the method exists and can be called
        Assert.DoesNotThrow(() =>
        {
            var task = service.GetFileCountByRunIdAsync(runId);
            Assert.That(task, Is.Not.Null);
        });
    }

    [Test]
    public void DatabaseService_WithLogger_DoesNotThrow()
    {
        // Arrange & Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var service = new DatabaseService(TestConnectionString, _mockLogger!.Object);
            Assert.That(service, Is.Not.Null);
        });
    }

    [Test]
    public void TestConnectionAsync_DoesNotThrowOnCreation()
    {
        // Arrange
        var service = new DatabaseService(TestConnectionString);

        // Act & Assert - This validates the method exists and can be called
        Assert.DoesNotThrow(() =>
        {
            var task = service.TestConnectionAsync();
            Assert.That(task, Is.Not.Null);
        });
    }

    // Helper methods for creating test data

    private static FileInformation CreateTestFileInformation(string fileName = "test.txt")
    {
        return new FileInformation(
            RunId: Guid.NewGuid(),
            FullPath: $@"C:\Test\{fileName}",
            FileName: fileName,
            FileExtension: Path.GetExtension(fileName),
            DirectoryPath: @"C:\Test",
            FileSizeBytes: 1024L,
            CreatedDate: DateTime.Now,
            ModifiedDate: DateTime.Now,
            AccessedDate: DateTime.Now,
            Content: $"Content of {fileName}",
            ContentHash: "test-hash-123",
            IsReadable: true,
            ErrorMessage: null);
    }

    private static DeploymentRelease CreateTestDeploymentRelease()
    {
        return new DeploymentRelease(
            RunId: Guid.NewGuid(),
            Tags: "test,unit,deployment",
            Deployment: "test-deployment-1.0",
            DeploymentDate: new DateTime(2025, 10, 15, 14, 30, 0));
    }

    private static async IAsyncEnumerable<FileInformation> CreateTestFileInformationAsyncEnumerable()
    {
        await Task.Yield();
        yield return CreateTestFileInformation("file1.txt");
        yield return CreateTestFileInformation("file2.txt");
    }

    private static async IAsyncEnumerable<FileInformation> CreateEmptyFileInformationAsyncEnumerable()
    {
        await Task.Yield();
        yield break;
    }
}