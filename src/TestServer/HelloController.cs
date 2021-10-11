using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using System;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace TestServer
{
    public class HelloController : Controller
    {
        private readonly GitHubClient _github;
        private readonly ILogger<HelloController> _logger;

        public HelloController(
            GitHubClient github,
            ILogger<HelloController> logger)
        {
            _github = github;
            _logger = logger;
        }

        [Authorize]
        public IActionResult World()
        {
            return Ok("Hello");
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

                    using var entryStream = entry.Open();

                    try
                    {
                        var packageReader = new PackageArchiveReader(entryStream);
                        var identity = packageReader.GetIdentity();

                        _logger.LogInformation(
                            "Indexing package {PackageId} {PackageVersion} in entry {Entry} in artifact {ArtifactName}...",
                            identity.Id,
                            identity.Version.ToNormalizedString(),
                            entry.FullName,
                            artifact.Name);

                        // ...

                        _logger.LogInformation(
                            "Indexed package {PackageId} {PackageVersion} in entry {Entry} in artifact {ArtifactName}...",
                            identity.Id,
                            identity.Version.ToNormalizedString(),
                            entry.FullName,
                            artifact.Name);


                    }
                    catch (Exception)
                    {
                        _logger.LogDebug(
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

    // See: https://docs.github.com/en/developers/webhooks-and-events/webhooks/webhook-events-and-payloads#workflow_run
    public class WorkflowRunEvent
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("workflow_run")]
        public WorkflowRun WorkflowRun { get; set; }

        [JsonPropertyName("repository")]
        public Repository Repository { get; set; }
    }

    public class WorkflowRun
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("conclusion")]
        public string Conclusion { get; set; }
    }

    public class Repository
    {
        [JsonPropertyName("full_name")]
        public string FullName { get; set; }

        [JsonPropertyName("name")]
        public string Name {get; set; }

        [JsonPropertyName("owner")]
        public Owner Owner { get; set; }
    }

    public class Owner
    {
        [JsonPropertyName("login")]
        public string Login { get; set; }
    }
}
