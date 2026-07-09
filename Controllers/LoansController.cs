using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;
using PrestexaAPI.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Security.Cryptography;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoansController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ManualMortgageApplicationService _manualMortgageApplicationService;

        public LoansController(
            AppDbContext context,
            ManualMortgageApplicationService manualMortgageApplicationService)
        {
            _context = context;
            _manualMortgageApplicationService = manualMortgageApplicationService;
        }

        [HttpGet("1003")]
        public async Task<IActionResult> Get1003List([FromQuery] bool includeArchived = false)
        {
            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var query = BuildLoanAccessQuery(userId, companyNmlsNumber, role);

            if (!includeArchived)
                query = query.Where(l => l.Status != LoanStatus.Archived);

            var loans = await query
                .OrderByDescending(l => l.CreatedAt)
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
                    isArchived = l.Status == LoanStatus.Archived,
                    l.CreatedAt,
                    l.UpdatedAt
                })
                .ToListAsync();

            return Ok(loans);
        }

        [HttpPost("1003")]
        public async Task<IActionResult> Create1003(
            [FromBody] ManualMortgageApplicationRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out _))
                return Unauthorized("Missing required token claims.");

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized("CompanyNmlsNumber claim is required.");

            var result = await _manualMortgageApplicationService.CreateAsync(
                request,
                userId,
                companyNmlsNumber,
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("1003/{loanNumber}")]
        public async Task<IActionResult> Get1003(string loanNumber, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out _, out var companyNmlsNumber, out _))
                return Unauthorized("Missing required token claims.");

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized("CompanyNmlsNumber claim is required.");

            var result = await _manualMortgageApplicationService.GetAsync(
                loanNumber,
                companyNmlsNumber,
                cancellationToken);

            if (result == null)
                return NotFound("Loan application not found.");

            return Ok(result);
        }

        [HttpPut("1003/{loanNumber}")]
        public async Task<IActionResult> Update1003(
            string loanNumber,
            [FromBody] ManualMortgageApplicationRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentUser(out _, out var companyNmlsNumber, out _))
                return Unauthorized("Missing required token claims.");

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized("CompanyNmlsNumber claim is required.");

            var result = await _manualMortgageApplicationService.UpdateAsync(
                loanNumber,
                request,
                companyNmlsNumber,
                cancellationToken);

            if (result == null)
                return NotFound("Loan application not found.");

            return Ok(result);
        }

        [HttpPatch("1003/{loanNumber}/archive")]
        public async Task<IActionResult> SetArchiveState1003(
            string loanNumber,
            [FromQuery] bool archived = true,
            [FromQuery] LoanStatus restoreStatus = LoanStatus.Pending,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var query = BuildLoanAccessQuery(userId, companyNmlsNumber, role);
            var loan = await query.FirstOrDefaultAsync(l => l.LoanNumber == loanNumber, cancellationToken);

            if (loan == null)
                return NotFound("Loan application not found.");

            loan.Status = archived ? LoanStatus.Archived : restoreStatus;
            loan.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                loan.LoanNumber,
                loan.Status,
                isArchived = loan.Status == LoanStatus.Archived,
                loan.UpdatedAt
            });
        }

        private async Task<string> GenerateLoanNumberAsync()
        {
            string loanNumber;

            do
            {
                var number = RandomNumberGenerator.GetInt32(10000000, 100000000);
                loanNumber = number.ToString();
            }
            while (await _context.Loans.AnyAsync(l => l.LoanNumber == loanNumber));

            return loanNumber;
        }

        private IQueryable<Loan> BuildLoanAccessQuery(int userId, string? companyNmlsNumber, string role)
        {
            var query = _context.Loans.AsQueryable();

            if (!IsSuperAdmin(role))
            {
                if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                    return query.Where(l => false);

                query = query.Where(l => l.CompanyNmlsNumber == companyNmlsNumber);
            }

            if (IsLoanOfficer(role))
                query = query.Where(l => l.UserId == userId);

            return query;
        }

        private bool TryGetCurrentUser(out int userId, out string? companyNmlsNumber, out string role)
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