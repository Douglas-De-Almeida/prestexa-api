using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DisclosuresController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetDisclosures()
        {
            return Ok(new
            {
                module = "Disclosures",
                message = "Disclosure forms module placeholder.",
                status = "Not implemented yet"
            });
        }
    }
}