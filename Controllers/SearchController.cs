using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SearchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SearchController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("loans")]
        public async Task<IActionResult> SearchLoans(
            string? loanNumber,
            string? subjectAddress,
            string? city,
            string? state,
            string? zipCode,
            string? status)
        {
            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var query = BuildLoanAccessQuery(userId, companyNmlsNumber, role);

            if (!string.IsNullOrWhiteSpace(loanNumber))
            {
                query = query.Where(l => l.LoanNumber.Contains(loanNumber));
            }

            if (!string.IsNullOrWhiteSpace(subjectAddress))
            {
                query = query.Where(l => l.Subject_Street_Address.Contains(subjectAddress));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(l => l.Subject_City.Contains(city));
            }

            if (!string.IsNullOrWhiteSpace(state))
            {
                query = query.Where(l => l.Subject_State == state);
            }

            if (!string.IsNullOrWhiteSpace(zipCode))
            {
                query = query.Where(l => l.Subject_ZipCode.Contains(zipCode));
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse(status, true, out LoanStatus parsedStatus))
            {
                query = query.Where(l => l.Status == parsedStatus);
            }

            var loans = await query
                .Select(l => new
                {
                    l.LoanNumber,
                    l.CompanyNmlsNumber,
                    l.UserId,
                    l.Subject_Street_Address,
                    l.Subject_City,
                    l.Subject_State,
                    l.Subject_ZipCode,
                    l.LoanAmount,
                    l.Status,
                    l.CreatedAt,
                    l.UpdatedAt
                })
                .ToListAsync();

            return Ok(loans);
        }

        private IQueryable<Loan> BuildLoanAccessQuery(
            int userId,
            string? companyNmlsNumber,
            string role)
        {
            var query = _context.Loans.AsQueryable();

            if (!IsSuperAdmin(role))
            {
                if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                    return query.Where(l => false);

                query = query.Where(l => l.CompanyNmlsNumber == companyNmlsNumber);
            }

            if (IsLoanOfficer(role))
            {
                query = query.Where(l => l.UserId == userId);
            }

            return query;
        }

        private bool TryGetCurrentUser(
            out int userId,
            out string? companyNmlsNumber,
            out string role)
        {
            userId = 0;
            companyNmlsNumber = null;
            role = string.Empty;

            var userIdValue = User.FindFirst("UserId")?.Value;
            companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;
            var roleValue = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(userIdValue, out userId))
                return false;

            role = roleValue ?? string.Empty;

            return true;
        }

        private static bool IsSuperAdmin(string role)
        {
            return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLoanOfficer(string role)
        {
            return string.Equals(role, "LoanOfficer", StringComparison.OrdinalIgnoreCase);
        }
    }
}