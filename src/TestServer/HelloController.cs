using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestServer
{
    public class HelloController : Controller
    {
        private readonly ILogger<HelloController> _logger;

        public HelloController(ILogger<HelloController> logger)
        {
            _logger = logger;
        }

        [Authorize]
        public IActionResult World()
        {
            return Ok("Hello");
        }

        [HttpPost]
        public IActionResult GitHubWebhook([FromBody] WorkflowRunEvent e)
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
                e.Repository.FullName);

            return Json(e);
        }
    }

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
        public long Id { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("conclusion")]
        public string Conclusion { get; set; }
    }

    public class Repository
    {
        [JsonPropertyName("full_name")]
        public string FullName { get; set; }
    }
}
