namespace ReleaseCodeCollector.Models;

/// <summary>
/// Represents comprehensive information about a file including its metadata and content.
/// </summary>
/// <param name="RunId">Unique identifier for the execution run that processed this file</param>
/// <param name="FullPath">The complete file path</param>
/// <param name="FileName">The name of the file including extension</param>
/// <param name="FileExtension">The file extension (e.g., .cs, .txt)</param>
/// <param name="DirectoryPath">The directory containing the file</param>
/// <param name="FileSizeBytes">The size of the file in bytes</param>
/// <param name="CreatedDate">The date when the file was created</param>
/// <param name="ModifiedDate">The date when the file was last modified</param>
/// <param name="AccessedDate">The date when the file was last accessed</param>
/// <param name="Content">The textual content of the file</param>
/// <param name="ContentHash">SHA256 hash of the file content for integrity verification</param>
/// <param name="IsReadable">Indicates whether the file content was successfully read</param>
/// <param name="ErrorMessage">Error message if file processing failed</param>
public record FileInformation(
    Guid RunId,
    string FullPath,
    string FileName,
    string FileExtension,
    string DirectoryPath,
    long FileSizeBytes,
    DateTime CreatedDate,
    DateTime ModifiedDate,
    DateTime AccessedDate,
    string? Content,
    string? ContentHash,
    bool IsReadable,
    string? ErrorMessage);