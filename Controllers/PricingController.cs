using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PricingController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetPricing()
        {
            return Ok(new
            {
                module = "Products & Pricing",
                message = "Pricing module placeholder.",
                status = "Not implemented yet"
            });
        }
    }
}