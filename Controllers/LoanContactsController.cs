using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoanContactsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetLoanContacts()
        {
            return Ok(new
            {
                module = "Loan Contacts",
                message = "Loan contacts module placeholder.",
                status = "Not implemented yet"
            });
        }
    }
}