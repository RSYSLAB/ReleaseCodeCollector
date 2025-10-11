using NUnit.Framework;
using ReleaseCodeCollector.Services;
using ReleaseCodeCollector.Models;
using System.Text;

namespace ReleaseCodeCollector.Tests.Services;

/// <summary>
/// Unit tests for the FileDiscoveryService class.
/// </summary>
[TestFixture]
public class FileDiscoveryServiceTests
{
    private FileDiscoveryService _service = null!;
    private string _testDirectory = null!;
    private Guid _testRunId;

    [SetUp]
    public void SetUp()
    {
        _service = new FileDiscoveryService();
        _testRunId = Guid.NewGuid();
        _testDirectory = Path.Combine(Path.GetTempPath(), "FileDiscoveryTest_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_testDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Test]
    public async Task DiscoverFilesAsync_WithEmptyDirectory_ReturnsEmpty()
    {
        // Act
        var files = new List<FileInformation>();
        await foreach (var file in _service.DiscoverFilesAsync(_testRunId, _testDirectory))
        {
            files.Add(file);
        }

        // Assert
        Assert.That(files, Is.Empty);
    }

    [Test]
    public async Task DiscoverFilesAsync_WithSingleTextFile_ReturnsFileInfo()
    {
        // Arrange
        var fileName = "test.txt";
        var filePath = Path.Combine(_testDirectory, fileName);
        var content = "Hello, World!";
        await File.WriteAllTextAsync(filePath, content);

        // Act
        var files = new List<FileInformation>();
        await foreach (var file in _service.DiscoverFilesAsync(_testRunId, _testDirectory))
        {
            files.Add(file);
        }

        // Assert
        Assert.That(files, Has.Count.EqualTo(1));
        var fileInfo = files[0];
        Assert.That(fileInfo.RunId, Is.EqualTo(_testRunId));
        Assert.That(fileInfo.FileName, Is.EqualTo(fileName));
        Assert.That(fileInfo.FileExtension, Is.EqualTo(".txt"));
        Assert.That(fileInfo.FullPath, Is.EqualTo(filePath));
        Assert.That(fileInfo.DirectoryPath, Is.EqualTo(_testDirectory));
        Assert.That(fileInfo.Content, Is.EqualTo(content));
        Assert.That(fileInfo.IsReadable, Is.True);
        Assert.That(fileInfo.ContentHash, Is.Not.Null);
        Assert.That(fileInfo.ErrorMessage, Is.Null);
    }

    [Test]
    public async Task DiscoverFilesAsync_WithMultipleFiles_ReturnsAllFiles()
    {
        // Arrange
        var files = new[] { "file1.txt", "file2.cs", "file3.json" };
        foreach (var fileName in files)
        {
            var filePath = Path.Combine(_testDirectory, fileName);
            await File.WriteAllTextAsync(filePath, $"Content of {fileName}");
        }

        // Act
        var discoveredFiles = new List<FileInformation>();
        await foreach (var file in _service.DiscoverFilesAsync(_testRunId, _testDirectory))
        {
            discoveredFiles.Add(file);
        }

        // Assert
        Assert.That(discoveredFiles, Has.Count.EqualTo(3));

        var fileNames = discoveredFiles.Select(f => f.FileName).ToList();
        Assert.That(fileNames, Does.Contain("file1.txt"));
        Assert.That(fileNames, Does.Contain("file2.cs"));
        Assert.That(fileNames, Does.Contain("file3.json"));
    }

    [Test]
    public async Task DiscoverFilesAsync_WithSubdirectories_ReturnsAllFilesRecursively()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "SubDirectory");
        Directory.CreateDirectory(subDir);

        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "root.txt"), "Root content");
        await File.WriteAllTextAsync(Path.Combine(subDir, "sub.txt"), "Sub content");

        // Act
        var files = new List<FileInformation>();
        await foreach (var file in _service.DiscoverFilesAsync(_testRunId, _testDirectory))
        {
            files.Add(file);
        }

        // Assert
        Assert.That(files, Has.Count.EqualTo(2));

        var rootFile = files.FirstOrDefault(f => f.FileName == "root.txt");
        var subFile = files.FirstOrDefault(f => f.FileName == "sub.txt");

        Assert.That(rootFile, Is.Not.Null);
        Assert.That(subFile, Is.Not.Null);
        Assert.That(rootFile!.DirectoryPath, Is.EqualTo(_testDirectory));
        Assert.That(subFile!.DirectoryPath, Is.EqualTo(subDir));
    }

    [Test]
    public async Task DiscoverFilesAsync_WithBinaryFile_SkipsContent()
    {
        // Arrange
        var fileName = "binary.exe";
        var filePath = Path.Combine(_testDirectory, fileName);
        var binaryContent = new byte[] { 0x4D, 0x5A, 0x90, 0x00 }; // PE header
        await File.WriteAllBytesAsync(filePath, binaryContent);

        // Act
        var files = new List<FileInformation>();
        await foreach (var file in _service.DiscoverFilesAsync(_testRunId, _testDirectory))
        {
            files.Add(file);
        }

        // Assert
        Assert.That(files, Has.Count.EqualTo(1));
        var fileInfo = files[0];
        Assert.That(fileInfo.FileName, Is.EqualTo(fileName));
        Assert.That(fileInfo.Content, Is.Null);
        Assert.That(fileInfo.ContentHash, Is.Null);
        Assert.That(fileInfo.IsReadable, Is.False);
        Assert.That(fileInfo.ErrorMessage, Is.EqualTo("Binary file - content not read"));
    }

    [Test]
    [TestCase(".dll")]
    [TestCase(".exe")]
    [TestCase(".jpg")]
    [TestCase(".png")]
    [TestCase(".pdf")]
    [TestCase(".zip")]
    public async Task DiscoverFilesAsync_WithBinaryExtensions_SkipsContent(string extension)
    {
        // Arrange
        var fileName = $"test{extension}";
        var filePath = Path.Combine(_testDirectory, fileName);
        await File.WriteAllBytesAsync(filePath, new byte[] { 1, 2, 3, 4 });

        // Act
        var files = new List<FileInformation>();
        await foreach (var file in _service.DiscoverFilesAsync(_testRunId, _testDirectory))
        {
            files.Add(file);
        }

        // Assert
        Assert.That(files, Has.Count.EqualTo(1));
        var fileInfo = files[0];
        Assert.That(fileInfo.IsReadable, Is.False);
        Assert.That(fileInfo.Content, Is.Null);
        Assert.That(fileInfo.ErrorMessage, Is.EqualTo("Binary file - content not read"));
    }

    [Test]
    public async Task DiscoverFilesAsync_WithLargeFile_SkipsContent()
    {
        // Arrange
        var fileName = "large.txt";
        var filePath = Path.Combine(_testDirectory, fileName);

        // Create a file larger than 100MB by writing a large string
        var largeContent = new string('A', 50 * 1024 * 1024); // 50MB of 'A' characters
        await File.WriteAllTextAsync(filePath, largeContent + largeContent); // 100MB+

        // Act
        var files = new List<FileInformation>();
        await foreach (var file in _service.DiscoverFilesAsync(_testRunId, _testDirectory))
        {
            files.Add(file);
        }

        // Assert
        Assert.That(files, Has.Count.EqualTo(1));
        var fileInfo = files[0];
        Assert.That(fileInfo.FileName, Is.EqualTo(fileName));
        Assert.That(fileInfo.Content, Is.Null);
        Assert.That(fileInfo.ContentHash, Is.Null);
        Assert.That(fileInfo.IsReadable, Is.False);
        Assert.That(fileInfo.ErrorMessage, Is.EqualTo("File too large - content not read"));
    }

    [Test]
    public async Task DiscoverFilesAsync_WithNullOrWhiteSpacePath_ThrowsException()
    {
        // Act & Assert - Empty string should throw ArgumentException
        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var file in _service.DiscoverFilesAsync(_testRunId, ""))
            {
                // This should not execute
            }
        });

        // Whitespace string should throw ArgumentException
        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (var file in _service.DiscoverFilesAsync(_testRunId, "   "))
            {
                // This should not execute
            }
        });

        // Null should throw ArgumentNullException
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await foreach (var file in _service.DiscoverFilesAsync(_testRunId, null!))
            {
                // This should not execute
            }
        });
    }

    [Test]
    public async Task DiscoverFilesAsync_WithNonExistentDirectory_ThrowsException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "NonExistent");

        // Act & Assert
        Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
        {
            await foreach (var file in _service.DiscoverFilesAsync(_testRunId, nonExistentPath))
            {
                // This should not execute
            }
        });
    }

    [Test]
    public async Task DiscoverFilesAsync_WithCancellation_RespondsToToken()
    {
        // Arrange
        var files = new[] { "file1.txt", "file2.txt", "file3.txt" };
        foreach (var fileName in files)
        {
            var filePath = Path.Combine(_testDirectory, fileName);
            await File.WriteAllTextAsync(filePath, $"Content of {fileName}");
        }

        using var cts = new CancellationTokenSource();

        // Act
        var discoveredFiles = new List<FileInformation>();
        var enumerator = _service.DiscoverFilesAsync(_testRunId, _testDirectory, cts.Token).GetAsyncEnumerator();

        try
        {
            // Get first file
            if (await enumerator.MoveNextAsync())
            {
                discoveredFiles.Add(enumerator.Current);
            }

            // Cancel before getting more files
            cts.Cancel();

            // This should throw OperationCanceledException
            Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                while (await enumerator.MoveNextAsync())
                {
                    discoveredFiles.Add(enumerator.Current);
                }
            });
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        // Assert
        Assert.That(discoveredFiles.Count, Is.LessThan(files.Length));
    }

    [Test]
    public async Task DiscoverFilesAsync_WithSpecialCharactersInFileName_HandlesCorrectly()
    {
        // Arrange
        var specialFiles = new[] { "file with spaces.txt", "file-with-dashes.txt", "file_with_underscores.txt" };
        foreach (var fileName in specialFiles)
        {
            var filePath = Path.Combine(_testDirectory, fileName);
            await File.WriteAllTextAsync(filePath, $"Content of {fileName}");
        }

        // Act
        var files = new List<FileInformation>();
        await foreach (var file in _service.DiscoverFilesAsync(_testRunId, _testDirectory))
        {
            files.Add(file);
        }

        // Assert
        Assert.That(files, Has.Count.EqualTo(3));

        var fileNames = files.Select(f => f.FileName).ToList();
        Assert.That(fileNames, Does.Contain("file with spaces.txt"));
        Assert.That(fileNames, Does.Contain("file-with-dashes.txt"));
        Assert.That(fileNames, Does.Contain("file_with_underscores.txt"));
    }

    [Test]
    public async Task DiscoverFilesAsync_WithEmptyFile_HandlesCorrectly()
    {
        // Arrange
        var fileName = "empty.txt";
        var filePath = Path.Combine(_testDirectory, fileName);
        await File.WriteAllTextAsync(filePath, string.Empty);

        // Act
        var files = new List<FileInformation>();
        await foreach (var file in _service.DiscoverFilesAsync(_testRunId, _testDirectory))
        {
            files.Add(file);
        }

        // Assert
        Assert.That(files, Has.Count.EqualTo(1));
        var fileInfo = files[0];
        Assert.That(fileInfo.Content, Is.EqualTo(string.Empty));
        Assert.That(fileInfo.IsReadable, Is.True);
        Assert.That(fileInfo.ContentHash, Is.Not.Null); // Even empty content should have a hash
        Assert.That(fileInfo.FileSizeBytes, Is.EqualTo(0));
    }

    [Test]
    public async Task DiscoverFilesAsync_VerifiesFileMetadata()
    {
        // Arrange
        var fileName = "metadata_test.txt";
        var filePath = Path.Combine(_testDirectory, fileName);
        var content = "Test content for metadata verification";

        await File.WriteAllTextAsync(filePath, content);
        var fileInfo = new System.IO.FileInfo(filePath);

        // Act
        var files = new List<FileInformation>();
        await foreach (var file in _service.DiscoverFilesAsync(_testRunId, _testDirectory))
        {
            files.Add(file);
        }

        // Assert
        Assert.That(files, Has.Count.EqualTo(1));
        var discoveredFile = files[0];

        Assert.That(discoveredFile.FileName, Is.EqualTo(fileName));
        Assert.That(discoveredFile.FileExtension, Is.EqualTo(".txt"));
        Assert.That(discoveredFile.FileSizeBytes, Is.EqualTo(fileInfo.Length));
        Assert.That(discoveredFile.FullPath, Is.EqualTo(filePath));
        Assert.That(discoveredFile.DirectoryPath, Is.EqualTo(_testDirectory));

        // Verify timestamps are reasonable (within last minute)
        var now = DateTime.Now;
        Assert.That(discoveredFile.CreatedDate, Is.LessThan(now));
        Assert.That(discoveredFile.ModifiedDate, Is.LessThan(now));
        Assert.That(discoveredFile.AccessedDate, Is.LessThan(now));

        Assert.That(discoveredFile.CreatedDate, Is.GreaterThan(now.AddMinutes(-1)));
    }

    [Test]
    public async Task DiscoverFilesAsync_WithUnicodeContent_HandlesCorrectly()
    {
        // Arrange
        var fileName = "unicode.txt";
        var filePath = Path.Combine(_testDirectory, fileName);
        var unicodeContent = "Hello 世界! 🌍 Привет мир! 🇺🇸";
        await File.WriteAllTextAsync(filePath, unicodeContent, Encoding.UTF8);

        // Act
        var files = new List<FileInformation>();
        await foreach (var file in _service.DiscoverFilesAsync(_testRunId, _testDirectory))
        {
            files.Add(file);
        }

        // Assert
        Assert.That(files, Has.Count.EqualTo(1));
        var fileInfo = files[0];
        Assert.That(fileInfo.Content, Is.EqualTo(unicodeContent));
        Assert.That(fileInfo.IsReadable, Is.True);
    }

    [Test]
    public async Task DiscoverFilesAsync_ContentHashConsistency()
    {
        // Arrange
        var fileName = "hash_test.txt";
        var filePath = Path.Combine(_testDirectory, fileName);
        var content = "Consistent content for hash testing";
        await File.WriteAllTextAsync(filePath, content);

        // Act - Run discovery twice
        var files1 = new List<FileInformation>();
        await foreach (var file in _service.DiscoverFilesAsync(_testRunId, _testDirectory))
        {
            files1.Add(file);
        }

        var files2 = new List<FileInformation>();
        await foreach (var file in _service.DiscoverFilesAsync(_testRunId, _testDirectory))
        {
            files2.Add(file);
        }

        // Assert
        Assert.That(files1, Has.Count.EqualTo(1));
        Assert.That(files2, Has.Count.EqualTo(1));
        Assert.That(files1[0].ContentHash, Is.EqualTo(files2[0].ContentHash));
        Assert.That(files1[0].ContentHash, Is.Not.Null);
        Assert.That(files1[0].ContentHash, Is.Not.Empty);
    }
}