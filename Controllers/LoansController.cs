using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;
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

        public LoansController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateLoan([FromBody] CreateLoanRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized("CompanyNmlsNumber claim is required.");

            var loan = new Loan
            {
                LoanNumber = await GenerateLoanNumberAsync(),
                CompanyNmlsNumber = companyNmlsNumber,
                UserId = userId,

                Subject_Street_Address = request.Subject_Street_Address,
                Subject_City = request.Subject_City,
                Subject_State = request.Subject_State,
                Subject_ZipCode = request.Subject_ZipCode,

                LoanAmount = request.LoanAmount,
                Status = LoanStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                loan.LoanNumber,
                loan.CompanyNmlsNumber,
                loan.UserId,
                loan.Subject_Street_Address,
                loan.Subject_City,
                loan.Subject_State,
                loan.Subject_ZipCode,
                loan.LoanAmount,
                loan.Status,
                loan.CreatedAt
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetLoans()
        {
            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var query = BuildLoanAccessQuery(userId, companyNmlsNumber, role);

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

        [HttpGet("{loanNumber}")]
        public async Task<IActionResult> GetLoan(string loanNumber)
        {
            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var query = BuildLoanAccessQuery(userId, companyNmlsNumber, role);

            var loan = await query
                .Where(l => l.LoanNumber == loanNumber)
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
                .FirstOrDefaultAsync();

            if (loan == null)
                return NotFound("Loan not found.");

            return Ok(loan);
        }

        [HttpPut("{loanNumber}")]
        public async Task<IActionResult> UpdateLoan(string loanNumber, [FromBody] UpdateLoanRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var query = BuildLoanAccessQuery(userId, companyNmlsNumber, role);

            var loan = await query.FirstOrDefaultAsync(l => l.LoanNumber == loanNumber);

            if (loan == null)
                return NotFound("Loan not found.");

            loan.Subject_Street_Address = request.Subject_Street_Address;
            loan.Subject_City = request.Subject_City;
            loan.Subject_State = request.Subject_State;
            loan.Subject_ZipCode = request.Subject_ZipCode;
            loan.LoanAmount = request.LoanAmount;
            loan.UpdatedAt = DateTime.UtcNow;

            if (!Enum.TryParse(request.Status, true, out LoanStatus status))
                return BadRequest("Invalid loan status.");

            loan.Status = status;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                loan.LoanNumber,
                loan.CompanyNmlsNumber,
                loan.UserId,
                loan.Subject_Street_Address,
                loan.Subject_City,
                loan.Subject_State,
                loan.Subject_ZipCode,
                loan.LoanAmount,
                loan.Status,
                loan.CreatedAt,
                loan.UpdatedAt
            });
        }

        [HttpDelete("{loanNumber}")]
        public async Task<IActionResult> DeleteLoan(string loanNumber)
        {
            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var query = BuildLoanAccessQuery(userId, companyNmlsNumber, role);

            var loan = await query.FirstOrDefaultAsync(l => l.LoanNumber == loanNumber);

            if (loan == null)
                return NotFound("Loan not found.");

            _context.Loans.Remove(loan);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Loan deleted successfully." });
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