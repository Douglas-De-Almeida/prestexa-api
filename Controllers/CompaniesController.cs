using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CompaniesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentCompany()
        {
            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized("CompanyNmlsNumber claim is required.");

            var company = await _context.Companies
                .Where(c => c.NmlsNumber == companyNmlsNumber)
                .Select(c => new
                {
                    c.Id,
                    c.NmlsNumber,
                    c.Name,
                    c.StreetAddress,
                    c.AptUnit,
                    c.City,
                    c.State,
                    c.ZipCode,
                    c.Email,
                    c.Phone,
                    c.WebsiteUrl,
                    c.PrimaryColor,
                    c.DbaBrandingEnabled,
                    c.IsActive,
                    c.CreatedAt,
                    c.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (company == null)
                return NotFound("Company not found.");

            return Ok(company);
        }
    }
}