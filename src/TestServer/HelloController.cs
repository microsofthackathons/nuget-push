using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace TestServer
{
    public class HelloController : Controller
    {
        [Authorize]
        public IActionResult World()
        {
            return Ok("Hello");
        }

        [HttpPost]
        public IActionResult GitHubWebhook([FromBody] WorkflowJobEvent e)
        {
            return Json(e);
        }
    }

    public class WorkflowJobEvent
    {
        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("workflow_job")]
        public WorkflowJob WorkflowJob { get; set; }

        [JsonPropertyName("repository")]
        public Repository Repository { get; set; }
    }

    public class WorkflowJob
    {
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
