using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TestServer
{
    public class HelloController : ControllerBase
    {
        [Authorize]
        public IActionResult World()
        {
            return Ok("Hello");
        }
    }
}
