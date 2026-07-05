using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FinancialInfoController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetFinancialInfo()
        {
            return Ok(new
            {
                module = "Financial Info",
                message = "Borrower financial information module placeholder.",
                status = "Not implemented yet"
            });
        }
    }
}