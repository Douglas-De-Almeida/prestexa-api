using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyBrandingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                message = "Use /api/company/assets/BrandingLogo or /api/company/assets/ProfilePicture."
            });
        }
    }
}