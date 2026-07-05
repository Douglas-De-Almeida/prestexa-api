using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoanCenterController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetLoanCenter()
        {
            return Ok(new
            {
                module = "Loan Center",
                message = "Loan center module placeholder.",
                status = "Not implemented yet"
            });
        }
    }
}