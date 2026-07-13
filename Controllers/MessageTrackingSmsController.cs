using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrestexaAPI.Models.Responses;
using System.Security.Claims;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/settings/message-tracking/sms")]
    public class MessageTrackingSmsController : ControllerBase
    {
        private static readonly string[] AdminRoles = ["Owner", "Company Admin", "Associate Admin", "Branch Admin", "SuperAdmin"];

        [HttpGet]
        public IActionResult GetSmsTracking([FromQuery] string? companyNmlsNumber = null)
        {
            if (!TryGetCurrentUser(out _, out var currentCompanyNmls, out var role))
                return Unauthorized();

            if (!AdminRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                return Forbid();

            if (string.Equals(User.FindFirst(ClaimTypes.Role)?.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(companyNmlsNumber) && !string.Equals(companyNmlsNumber.Trim(), currentCompanyNmls, StringComparison.Ordinal))
                return Ok(new SmsTrackingListResponse());

            return Ok(new SmsTrackingListResponse { Page = 1, PageSize = 25, TotalCount = 0, Items = [] });
        }

        private bool TryGetCurrentUser(out int userId, out string? companyNmlsNumber, out string role)
        {
            userId = 0;
            companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;
            role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var userIdValue = User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdValue, out userId);
        }
    }
}