namespace ReleaseCodeCollector.Models;

/// <summary>
/// Represents deployment release information with associated metadata.
/// </summary>
/// <param name="RunId">Unique identifier for the execution run that created this deployment release</param>
/// <param name="Tags">Tags associated with the deployment release (e.g., version, environment)</param>
/// <param name="Deployment">The deployment name or identifier</param>
/// <param name="DeploymentDate">The date when the deployment was created or executed</param>
public record DeploymentRelease(
    Guid RunId,
    string Tags,
    string Deployment,
    DateTime DeploymentDate);