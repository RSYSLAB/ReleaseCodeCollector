using NUnit.Framework;
using ReleaseCodeCollector.Models;

namespace ReleaseCodeCollector.Tests.Models;

/// <summary>
/// Unit tests for the DeploymentRelease record.
/// </summary>
[TestFixture]
public class DeploymentReleaseTests
{
    [Test]
    public void DeploymentRelease_Constructor_SetsAllProperties()
    {
        // Arrange
        var runId = Guid.NewGuid();
        var tags = "v1.0,production,release";
        var deployment = "production-release-2025-10-15";
        var deploymentDate = new DateTime(2025, 10, 15, 14, 30, 0);

        // Act
        var deploymentRelease = new DeploymentRelease(
            runId,
            tags,
            deployment,
            deploymentDate);

        // Assert
        Assert.That(deploymentRelease.RunId, Is.EqualTo(runId));
        Assert.That(deploymentRelease.Tags, Is.EqualTo(tags));
        Assert.That(deploymentRelease.Deployment, Is.EqualTo(deployment));
        Assert.That(deploymentRelease.DeploymentDate, Is.EqualTo(deploymentDate));
    }

    [Test]
    public void DeploymentRelease_Equality_WorksCorrectly()
    {
        // Arrange
        var runId = Guid.NewGuid();
        var deploymentDate = new DateTime(2025, 10, 15, 14, 30, 0);

        var deploymentRelease1 = new DeploymentRelease(
            runId,
            "v1.0,production",
            "release-deployment",
            deploymentDate);

        var deploymentRelease2 = new DeploymentRelease(
            runId,
            "v1.0,production",
            "release-deployment",
            deploymentDate);

        // Act & Assert
        Assert.That(deploymentRelease1, Is.EqualTo(deploymentRelease2));
        Assert.That(deploymentRelease1.GetHashCode(), Is.EqualTo(deploymentRelease2.GetHashCode()));
    }

    [Test]
    public void DeploymentRelease_Inequality_WorksCorrectly()
    {
        // Arrange
        var runId1 = Guid.NewGuid();
        var runId2 = Guid.NewGuid();
        var deploymentDate = new DateTime(2025, 10, 15, 14, 30, 0);

        var deploymentRelease1 = new DeploymentRelease(
            runId1,
            "v1.0,production",
            "release-deployment",
            deploymentDate);

        var deploymentRelease2 = new DeploymentRelease(
            runId2,
            "v1.0,production",
            "release-deployment",
            deploymentDate);

        // Act & Assert
        Assert.That(deploymentRelease1, Is.Not.EqualTo(deploymentRelease2));
    }

    [Test]
    public void DeploymentRelease_WithDifferentTags_AreNotEqual()
    {
        // Arrange
        var runId = Guid.NewGuid();
        var deploymentDate = new DateTime(2025, 10, 15, 14, 30, 0);

        var deploymentRelease1 = new DeploymentRelease(
            runId,
            "v1.0,staging",
            "release-deployment",
            deploymentDate);

        var deploymentRelease2 = new DeploymentRelease(
            runId,
            "v1.0,production",
            "release-deployment",
            deploymentDate);

        // Act & Assert
        Assert.That(deploymentRelease1, Is.Not.EqualTo(deploymentRelease2));
    }

    [Test]
    public void DeploymentRelease_ToString_ContainsKeyInformation()
    {
        // Arrange
        var runId = Guid.NewGuid();
        var deploymentDate = new DateTime(2025, 10, 15, 14, 30, 0);
        var deploymentRelease = new DeploymentRelease(
            runId,
            "v1.0,production,critical",
            "production-release-2025-10-15",
            deploymentDate);

        // Act
        var result = deploymentRelease.ToString();

        // Assert
        Assert.That(result, Does.Contain("v1.0,production,critical"));
        Assert.That(result, Does.Contain("production-release-2025-10-15"));
        Assert.That(result, Does.Contain(runId.ToString()));
    }

    [Test]
    [TestCase("", "Valid deployment")]
    [TestCase(null, "Valid deployment")]
    [TestCase("Valid tags", "")]
    [TestCase("Valid tags", null)]
    public void DeploymentRelease_HandlesNullOrEmptyStrings(string tags, string deployment)
    {
        // Arrange
        var runId = Guid.NewGuid();
        var deploymentDate = new DateTime(2025, 10, 15, 14, 30, 0);

        // Act & Assert - Should not throw exceptions
        Assert.DoesNotThrow(() =>
        {
            var deploymentRelease = new DeploymentRelease(
                runId,
                tags,
                deployment,
                deploymentDate);

            Assert.That(deploymentRelease.Tags, Is.EqualTo(tags));
            Assert.That(deploymentRelease.Deployment, Is.EqualTo(deployment));
        });
    }
}