using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoanQuotesController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetLoanQuotes()
        {
            return Ok(new
            {
                module = "Loan Quotes",
                message = "Loan quotes module placeholder.",
                status = "Not implemented yet"
            });
        }
    }
}