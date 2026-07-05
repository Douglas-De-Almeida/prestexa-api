using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientNeedsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetClientNeeds()
        {
            return Ok(new
            {
                module = "Client Needs",
                message = "Client needs module placeholder.",
                status = "Not implemented yet"
            });
        }
    }
}