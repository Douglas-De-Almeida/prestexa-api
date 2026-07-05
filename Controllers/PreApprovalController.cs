using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PreApprovalController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetPreApproval()
        {
            return Ok(new
            {
                module = "Pre-Approval",
                message = "Pre-approval module placeholder.",
                status = "Not implemented yet"
            });
        }
    }
}