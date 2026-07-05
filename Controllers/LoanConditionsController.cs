using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoanConditionsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetLoanConditions()
        {
            return Ok(new
            {
                module = "Loan Conditions",
                message = "Loan conditions module placeholder.",
                status = "Not implemented yet"
            });
        }
    }
}