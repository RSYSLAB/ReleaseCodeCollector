using System.Security.Cryptography;
using System.Text;
using ReleaseCodeCollector.Models;

namespace ReleaseCodeCollector.Services;

/// <summary>
/// Service responsible for discovering and processing files within a specified directory.
/// </summary>
public class FileDiscoveryService
{
    private readonly HashSet<string> _binaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bin", ".obj", ".pdb", ".zip", ".rar", ".7z", ".tar", ".gz",
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".ico", ".svg", ".webp",
        ".mp3", ".mp4", ".avi", ".mkv", ".wav", ".flac", ".ogg",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"
    };

    /// <summary>
    /// Discovers all files under the specified path and returns their information.
    /// </summary>
    /// <param name="runId">Unique identifier for this execution run</param>
    /// <param name="rootPath">The root directory path to search</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>An enumerable collection of file information</returns>
    /// <exception cref="ArgumentException">Thrown when the path is null, empty, or invalid</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist</exception>
    public async IAsyncEnumerable<FileInformation> DiscoverFilesAsync(Guid runId, string rootPath, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {rootPath}");
        }

        var enumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            ReturnSpecialDirectories = false
        };

        await foreach (var filePath in EnumerateFilesAsync(rootPath, enumerationOptions, cancellationToken))
        {
            yield return await ProcessFileAsync(runId, filePath, cancellationToken);
        }
    }

    /// <summary>
    /// Asynchronously enumerates files in the specified directory.
    /// </summary>
    private static async IAsyncEnumerable<string> EnumerateFilesAsync(
        string path,
        EnumerationOptions options,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Ensure we're running asynchronously

        foreach (var file in Directory.EnumerateFiles(path, "*", options))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return file;
        }
    }

    /// <summary>
    /// Processes a single file and extracts its information.
    /// </summary>
    /// <param name="runId">Unique identifier for this execution run</param>
    /// <param name="filePath">The path to the file to process</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>File information including content if readable</returns>
    private async Task<FileInformation> ProcessFileAsync(Guid runId, string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var fileInfo = new System.IO.FileInfo(filePath);
            var fileName = fileInfo.Name;
            var fileExtension = fileInfo.Extension;
            var directoryPath = fileInfo.DirectoryName ?? string.Empty;
            var fileSizeBytes = fileInfo.Length;
            var createdDate = fileInfo.CreationTime;
            var modifiedDate = fileInfo.LastWriteTime;
            var accessedDate = fileInfo.LastAccessTime;

            // Determine if file should be read as text
            var isTextFile = !_binaryExtensions.Contains(fileExtension) && fileSizeBytes < 100 * 1024 * 1024; // Skip files larger than 100MB

            string? content = null;
            string? contentHash = null;
            var isReadable = false;
            string? errorMessage = null;

            if (isTextFile)
            {
                try
                {
                    content = await File.ReadAllTextAsync(filePath, cancellationToken);
                    contentHash = ComputeContentHash(content);
                    isReadable = true;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or OutOfMemoryException)
                {
                    errorMessage = $"Failed to read file content: {ex.Message}";
                    isReadable = false;
                }
            }
            else
            {
                errorMessage = _binaryExtensions.Contains(fileExtension)
                    ? "Binary file - content not read"
                    : "File too large - content not read";
                isReadable = false;
            }

            return new FileInformation(
                RunId: runId,
                FullPath: filePath,
                FileName: fileName,
                FileExtension: fileExtension,
                DirectoryPath: directoryPath,
                FileSizeBytes: fileSizeBytes,
                CreatedDate: createdDate,
                ModifiedDate: modifiedDate,
                AccessedDate: accessedDate,
                Content: content,
                ContentHash: contentHash,
                IsReadable: isReadable,
                ErrorMessage: errorMessage
            );
        }
        catch (Exception ex)
        {
            return new FileInformation(
                RunId: runId,
                FullPath: filePath,
                FileName: Path.GetFileName(filePath),
                FileExtension: Path.GetExtension(filePath),
                DirectoryPath: Path.GetDirectoryName(filePath) ?? string.Empty,
                FileSizeBytes: 0,
                CreatedDate: DateTime.MinValue,
                ModifiedDate: DateTime.MinValue,
                AccessedDate: DateTime.MinValue,
                Content: null,
                ContentHash: null,
                IsReadable: false,
                ErrorMessage: $"Failed to process file: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Computes SHA256 hash of the file content.
    /// </summary>
    /// <param name="content">The file content to hash</param>
    /// <returns>Base64 encoded SHA256 hash</returns>
    private static string ComputeContentHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}