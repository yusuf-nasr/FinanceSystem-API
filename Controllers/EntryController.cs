using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FinanceSystem_Dotnet.Controllers
{
    [Route("/")]
    [ApiController]
    public class EntryController : ControllerBase
    {
        [HttpGet]
        public ActionResult GetSystemInfo()
        {
            var process = Process.GetCurrentProcess();
            return Ok(new
            {
                name = "Tanta Financial System API",
                version = "1.0.0",
                description = "The API for the graduation project (.NET)",
                docs = "/swagger",
                timestamp = DateTime.UtcNow.ToString("o"),
                health = new
                {
                    status = "up",
                    uptime = (DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds,
                    memoryUsage = process.WorkingSet64 / (1024.0 * 1024.0)
                }
            });
        }
    }
}
