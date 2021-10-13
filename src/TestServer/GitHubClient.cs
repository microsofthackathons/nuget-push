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

namespace TestServer
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

            // Use a file so that the stream is seekable.
            // Use a temporary file that will be deleted when disposed.
            var artifactStream = new FileStream(
                Path.GetTempFileName(),
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.DeleteOnClose);

            using (var downloadStream = await response.Content.ReadAsStreamAsync(cancellation))
            {
                await downloadStream.CopyToAsync(artifactStream, cancellation);
                await downloadStream.FlushAsync();
            }

            // Rewind the seekable stream to the beginning.
            artifactStream.Position = 0;

            return artifactStream;
        }
    }

    public class GitHubOptions
    {
        public string Token { get; set; }
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
