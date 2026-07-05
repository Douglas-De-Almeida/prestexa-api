using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FundingController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetFunding()
        {
            return Ok(new
            {
                module = "Funding / Revenue",
                message = "Funding and revenue module placeholder.",
                status = "Not implemented yet"
            });
        }
    }
}