using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AusController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAus()
        {
            return Ok(new
            {
                module = "AUS",
                message = "Automated underwriting system module placeholder.",
                status = "Not implemented yet"
            });
        }
    }
}