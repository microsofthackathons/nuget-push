using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NuGetServer
{
    public class GitHubClient
    {
        private readonly HttpClient _http;
        private readonly IOptionsSnapshot<GitHubOptions> _options;
        private readonly ILogger<GitHubClient> _logger;

        public GitHubClient(
            HttpClient http,
            IOptionsSnapshot<GitHubOptions> options,
            ILogger<GitHubClient> logger)
        {
            _http = http;
            _options = options;
            _logger = logger;

            _http.BaseAddress = new Uri("https://api.github.com/");
            _http.DefaultRequestHeaders.Add("User-Agent", "NuGetPackageDownloader/1.0.0");
            _http.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            if (!string.IsNullOrEmpty(_options.Value.Token))
            {
                _http.DefaultRequestHeaders.Add("Authorization", $"token {_options.Value.Token}");
            }
        }

        // See: https://docs.github.com/en/rest/reference/actions#list-workflow-run-artifacts
        public async Task<WorkflowRunArtifactsResponse> ListWorkflowRunArtifactsAsync(
            string owner,
            string repo,
            int runId,
            CancellationToken cancellation)
        {
            return await _http.GetFromJsonAsync<WorkflowRunArtifactsResponse>(
                $"/repos/{owner}/{repo}/actions/runs/{runId}/artifacts",
                cancellation);
        }

        // See: https://docs.github.com/en/rest/reference/actions#download-an-artifact
        public async Task<Stream> DownloadArtifactAsync(
            string owner,
            string repo,
            int artifactId,
            CancellationToken cancellation)
        {
            var response = await _http.GetAsync(
                $"/repos/{owner}/{repo}/actions/artifacts/{artifactId}/zip",
                cancellation);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Unable to download artifact {ArtifactId}: {Response}",
                    artifactId,
                    await response.Content.ReadAsStringAsync(cancellation));

                response.EnsureSuccessStatusCode();
            }

            using (var downloadStream = await response.Content.ReadAsStreamAsync(cancellation))
            {
                return await downloadStream.ToTemporaryFileStreamAsync(cancellation);
            }
        }
    }

    public class GitHubOptions
    {
        public string Token { get; set; }
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
        public string Name { get; set; }

        [JsonPropertyName("owner")]
        public Owner Owner { get; set; }
    }

    public class Owner
    {
        [JsonPropertyName("login")]
        public string Login { get; set; }
    }

    // See: https://docs.github.com/en/rest/reference/actions#list-workflow-run-artifacts
    public class WorkflowRunArtifactsResponse
    {
        [JsonPropertyName("artifacts")]
        public List<WorkflowRunArtifact> Artifacts { get; set; }
    }

    public class WorkflowRunArtifact
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("archive_download_url")]
        public Uri ArchiveDownloadUrl { get; set; }
    }
}
