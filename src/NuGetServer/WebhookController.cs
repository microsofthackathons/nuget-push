using BaGet.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using System;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NuGetServer
{
    public class WebhookController : Controller
    {
        private readonly GitHubClient _github;
        private readonly IPackageIndexingService _packages;
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(
            GitHubClient github,
            IPackageIndexingService packages,
            ILogger<WebhookController> logger)
        {
            _github = github;
            _packages = packages;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> GitHubWebhook(
            [FromBody] WorkflowRunEvent e,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received webhook: {Webhook}", JsonSerializer.Serialize(e));

            if (e.Action != "completed" || e.WorkflowRun?.Status != "completed" || e.WorkflowRun?.Conclusion != "success")
            {
                _logger.LogWarning(
                    "Ignoring workflow run with action {Action}, status {Status}, conclusion {Conclusion}",
                    e.Action,
                    e.WorkflowRun?.Status,
                    e.WorkflowRun?.Conclusion);

                return Ok();
            }

            _logger.LogInformation(
                "Indexing artifacts from workflow run ID {WorkflowRunId} for repository {Repository}...",
                e.WorkflowRun.Id,
                e.Repository.Owner.Login + "/" + e.Repository.Name);

            var workflowResult = await _github.ListWorkflowRunArtifactsAsync(
                e.Repository.Owner.Login,
                e.Repository.Name,
                e.WorkflowRun.Id,
                cancellationToken);

            foreach (var artifact in workflowResult.Artifacts)
            {
                _logger.LogInformation(
                    "Indexing artifact {ArtifactName}...",
                    artifact.Name,
                    artifact.ArchiveDownloadUrl);

                using var artifactStream = await _github.DownloadArtifactAsync(
                    e.Repository.Owner.Login,
                    e.Repository.Name,
                    artifact.Id,
                    cancellationToken);

                using var zipReader = new ZipArchive(artifactStream, ZipArchiveMode.Read);

                foreach (var entry in zipReader.Entries)
                {
                    _logger.LogDebug(
                        "Indexing entry {Entry} from artifact {ArtifactName}...",
                        entry.FullName,
                        artifact.Name);

                    using var entryStream = await entry.Open().ToTemporaryFileStreamAsync(cancellationToken);

                    try
                    {
                        // Verify the stream contains a package...
                        string packageId;
                        string packageVersion;
                        using (var packageReader = new PackageArchiveReader(entryStream, leaveStreamOpen: true))
                        {
                            var identity = packageReader.GetIdentity();

                            packageId = identity.Id;
                            packageVersion = identity.Version.ToNormalizedString();
                        }

                        // No exception was thrown. The stream is indeed a package.
                        // Reset it and index its content.
                        entryStream.Position = 0;

                        _logger.LogInformation(
                            "Indexing package {PackageId} {PackageVersion} in entry {Entry} in artifact {ArtifactName}...",
                            packageId,
                            packageVersion,
                            entry.FullName,
                            artifact.Name);

                        await _packages.IndexAsync(entryStream, cancellationToken);

                        _logger.LogInformation(
                            "Indexed package {PackageId} {PackageVersion} in entry {Entry} in artifact {ArtifactName}...",
                            packageId,
                            packageVersion,
                            entry.FullName,
                            artifact.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to index entry {Entry} from artifact {ArtifactName}",
                            entry.FullName,
                            artifact.Name);
                    }
                }

                _logger.LogInformation(
                    "Indexed artifact {ArtifactName} at {ArtifactUrl}",
                    artifact.Name,
                    artifact.ArchiveDownloadUrl);
            }

            return Json(e);
        }
    }
}
