using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeesController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetFees()
        {
            return Ok(new
            {
                module = "Fees",
                message = "Review fees module placeholder.",
                status = "Not implemented yet"
            });
        }
    }
}