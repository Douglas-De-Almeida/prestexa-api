using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrestexaAPI.Models.Requests;
using PrestexaAPI.Services.Credit;
using System.Security.Claims;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/credit")]
    [Authorize]
    public class CreditController : ControllerBase
    {
        private readonly ICreditService _creditService;

        public CreditController(ICreditService creditService)
        {
            _creditService = creditService;
        }

        [HttpPost("order")]
        public async Task<IActionResult> Order(
            [FromBody] OrderCreditReportRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryBuildUserContext(out var user))
                return Unauthorized("Missing required token claims.");

            try
            {
                var result = await _creditService.OrderAsync(request, user, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("reports/{loanId}")]
        public async Task<IActionResult> GetReports(
            string loanId,
            CancellationToken cancellationToken)
        {
            if (!TryBuildUserContext(out var user))
                return Unauthorized("Missing required token claims.");

            try
            {
                var result = await _creditService.GetReportsAsync(loanId, user, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private bool TryBuildUserContext(out CreditUserContext user)
        {
            user = new CreditUserContext();

            var userIdValue = User.FindFirst("UserId")?.Value;
            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
                return false;

            user = new CreditUserContext
            {
                UserId = userId,
                CompanyNmlsNumber = companyNmlsNumber,
                Role = role ?? string.Empty,
                Name = name ?? string.Empty
            };

            return true;
        }
    }
}
