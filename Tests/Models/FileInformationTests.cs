using NUnit.Framework;
using ReleaseCodeCollector.Models;

namespace ReleaseCodeCollector.Tests.Models;

/// <summary>
/// Unit tests for the FileInformation record.
/// </summary>
[TestFixture]
public class FileInformationTests
{
    private readonly Guid _testRunId = Guid.NewGuid();
    private readonly DateTime _testDate = DateTime.Now;

    [Test]
    public void FileInformation_Constructor_SetsAllProperties()
    {
        // Arrange
        var runId = _testRunId;
        var fullPath = @"C:\Test\File.txt";
        var fileName = "File.txt";
        var fileExtension = ".txt";
        var directoryPath = @"C:\Test";
        var fileSizeBytes = 1024L;
        var createdDate = _testDate;
        var modifiedDate = _testDate.AddHours(1);
        var accessedDate = _testDate.AddHours(2);
        var content = "Test content";
        var contentHash = "abc123";
        var isReadable = true;
        var errorMessage = "No error";

        // Act
        var fileInfo = new FileInformation(
            runId,
            fullPath,
            fileName,
            fileExtension,
            directoryPath,
            fileSizeBytes,
            createdDate,
            modifiedDate,
            accessedDate,
            content,
            contentHash,
            isReadable,
            errorMessage);

        // Assert
        Assert.That(fileInfo.RunId, Is.EqualTo(runId));
        Assert.That(fileInfo.FullPath, Is.EqualTo(fullPath));
        Assert.That(fileInfo.FileName, Is.EqualTo(fileName));
        Assert.That(fileInfo.FileExtension, Is.EqualTo(fileExtension));
        Assert.That(fileInfo.DirectoryPath, Is.EqualTo(directoryPath));
        Assert.That(fileInfo.FileSizeBytes, Is.EqualTo(fileSizeBytes));
        Assert.That(fileInfo.CreatedDate, Is.EqualTo(createdDate));
        Assert.That(fileInfo.ModifiedDate, Is.EqualTo(modifiedDate));
        Assert.That(fileInfo.AccessedDate, Is.EqualTo(accessedDate));
        Assert.That(fileInfo.Content, Is.EqualTo(content));
        Assert.That(fileInfo.ContentHash, Is.EqualTo(contentHash));
        Assert.That(fileInfo.IsReadable, Is.EqualTo(isReadable));
        Assert.That(fileInfo.ErrorMessage, Is.EqualTo(errorMessage));
    }

    [Test]
    public void FileInformation_WithNullContent_HandlesNullValues()
    {
        // Arrange & Act
        var fileInfo = new FileInformation(
            _testRunId,
            @"C:\Test\Binary.exe",
            "Binary.exe",
            ".exe",
            @"C:\Test",
            2048L,
            _testDate,
            _testDate,
            _testDate,
            null, // Content is null for binary files
            null, // ContentHash is null
            false, // Not readable
            null); // No error

        // Assert
        Assert.That(fileInfo.Content, Is.Null);
        Assert.That(fileInfo.ContentHash, Is.Null);
        Assert.That(fileInfo.IsReadable, Is.False);
        Assert.That(fileInfo.ErrorMessage, Is.Null);
    }

    [Test]
    public void FileInformation_Equality_WorksCorrectly()
    {
        // Arrange
        var fileInfo1 = new FileInformation(
            _testRunId,
            @"C:\Test\File.txt",
            "File.txt",
            ".txt",
            @"C:\Test",
            1024L,
            _testDate,
            _testDate,
            _testDate,
            "content",
            "hash",
            true,
            null);

        var fileInfo2 = new FileInformation(
            _testRunId,
            @"C:\Test\File.txt",
            "File.txt",
            ".txt",
            @"C:\Test",
            1024L,
            _testDate,
            _testDate,
            _testDate,
            "content",
            "hash",
            true,
            null);

        // Act & Assert
        Assert.That(fileInfo1, Is.EqualTo(fileInfo2));
        Assert.That(fileInfo1.GetHashCode(), Is.EqualTo(fileInfo2.GetHashCode()));
    }

    [Test]
    public void FileInformation_Inequality_WorksCorrectly()
    {
        // Arrange
        var fileInfo1 = new FileInformation(
            _testRunId,
            @"C:\Test\File1.txt",
            "File1.txt",
            ".txt",
            @"C:\Test",
            1024L,
            _testDate,
            _testDate,
            _testDate,
            "content1",
            "hash1",
            true,
            null);

        var fileInfo2 = new FileInformation(
            _testRunId,
            @"C:\Test\File2.txt",
            "File2.txt",
            ".txt",
            @"C:\Test",
            2048L,
            _testDate,
            _testDate,
            _testDate,
            "content2",
            "hash2",
            true,
            null);

        // Act & Assert
        Assert.That(fileInfo1, Is.Not.EqualTo(fileInfo2));
    }

    [Test]
    public void FileInformation_ToString_ContainsKey信息()
    {
        // Arrange
        var fileInfo = new FileInformation(
            _testRunId,
            @"C:\Test\File.txt",
            "File.txt",
            ".txt",
            @"C:\Test",
            1024L,
            _testDate,
            _testDate,
            _testDate,
            "content",
            "hash",
            true,
            null);

        // Act
        var result = fileInfo.ToString();

        // Assert
        Assert.That(result, Does.Contain("File.txt"));
        Assert.That(result, Does.Contain(_testRunId.ToString()));
    }

    [Test]
    public void FileInformation_WithErrorMessage_PreservesError()
    {
        // Arrange
        var errorMessage = "Access denied";

        // Act
        var fileInfo = new FileInformation(
            _testRunId,
            @"C:\Protected\File.txt",
            "File.txt",
            ".txt",
            @"C:\Protected",
            0L,
            _testDate,
            _testDate,
            _testDate,
            null,
            null,
            false,
            errorMessage);

        // Assert
        Assert.That(fileInfo.ErrorMessage, Is.EqualTo(errorMessage));
        Assert.That(fileInfo.IsReadable, Is.False);
    }
}