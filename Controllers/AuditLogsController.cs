using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuditLogsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAuditLogs()
        {
            return Ok(new
            {
                module = "Audit Logs",
                message = "Audit logs module placeholder.",
                status = "Not implemented yet"
            });
        }
    }
}