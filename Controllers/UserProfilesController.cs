using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserProfilesController : ControllerBase
    {
        [HttpGet("me")]
        public IActionResult GetMyProfile()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            return Ok(new
            {
                module = "User Profiles",
                message = "User profile module placeholder.",
                status = "Not implemented yet",
                currentUser = new
                {
                    userId,
                    companyNmlsNumber,
                    role
                }
            });
        }
    }
}