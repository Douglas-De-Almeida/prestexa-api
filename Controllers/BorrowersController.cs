using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;
using System.Security.Claims;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class BorrowersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BorrowersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("loans/{loanNumber}/borrowers")]
        public async Task<IActionResult> GetBorrowers(string loanNumber)
        {
            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized();

            var loan = await _context.Loans
                .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber && l.CompanyNmlsNumber == companyNmlsNumber);

            if (loan == null)
                return NotFound("Loan not found.");

            var borrowers = await _context.Borrowers
                .Where(b => b.LoanId == loan.Id)
                .ToListAsync();

            return Ok(borrowers);
        }

        [HttpPost("loans/{loanNumber}/borrowers")]
        public async Task<IActionResult> CreateBorrower(string loanNumber, [FromBody] CreateBorrowerRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized();

            var loan = await _context.Loans
                .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber && l.CompanyNmlsNumber == companyNmlsNumber);

            if (loan == null)
                return NotFound("Loan not found.");

            var borrower = new Borrower
            {
                CompanyNmlsNumber = companyNmlsNumber,
                LoanId = loan.Id,
                BorrowerType = request.BorrowerType,
                FirstName = request.FirstName,
                MiddleName = request.MiddleName,
                LastName = request.LastName,
                Email = request.Email,
                CellPhone = request.CellPhone,
                DateOfBirth = request.DateOfBirth
            };

            _context.Borrowers.Add(borrower);
            await _context.SaveChangesAsync();

            return Ok(borrower);
        }

        [HttpPut("borrowers/{borrowerId}")]
        public async Task<IActionResult> UpdateBorrower(int borrowerId, [FromBody] UpdateBorrowerRequest request)
        {
            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized();

            var borrower = await _context.Borrowers
                .FirstOrDefaultAsync(b => b.Id == borrowerId && b.CompanyNmlsNumber == companyNmlsNumber);

            if (borrower == null)
                return NotFound("Borrower not found.");

            borrower.BorrowerType = request.BorrowerType;
            borrower.FirstName = request.FirstName;
            borrower.MiddleName = request.MiddleName;
            borrower.LastName = request.LastName;
            borrower.Email = request.Email;
            borrower.CellPhone = request.CellPhone;
            borrower.HomePhone = request.HomePhone;
            borrower.WorkPhone = request.WorkPhone;
            borrower.MaritalStatus = request.MaritalStatus;
            borrower.ResidencyType = request.ResidencyType;
            borrower.DateOfBirth = request.DateOfBirth;
            borrower.EstimatedCreditScore = request.EstimatedCreditScore;
            borrower.EConsentAuthorized = request.EConsentAuthorized;
            borrower.CreditPullAuthorized = request.CreditPullAuthorized;

            await _context.SaveChangesAsync();

            return Ok(borrower);
        }
    }
}