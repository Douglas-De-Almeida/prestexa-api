using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BranchesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BranchesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetBranches()
        {
            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized("CompanyNmlsNumber claim is required.");

            var branches = await _context.Branches
                .Where(b => b.CompanyNmlsNumber == companyNmlsNumber)
                .Select(b => new
                {
                    b.Id,
                    b.CompanyNmlsNumber,
                    b.Name,
                    b.BranchNmlsNumber,
                    b.StreetAddress,
                    b.City,
                    b.State,
                    b.ZipCode,
                    b.IsHq,
                    b.IsActive,
                    b.CreatedAt
                })
                .ToListAsync();

            return Ok(branches);
        }

        [HttpGet("{branchId}")]
        public async Task<IActionResult> GetBranch(int branchId)
        {
            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized("CompanyNmlsNumber claim is required.");

            var branch = await _context.Branches
                .Where(b => b.Id == branchId && b.CompanyNmlsNumber == companyNmlsNumber)
                .Select(b => new
                {
                    b.Id,
                    b.CompanyNmlsNumber,
                    b.Name,
                    b.BranchNmlsNumber,
                    b.StreetAddress,
                    b.City,
                    b.State,
                    b.ZipCode,
                    b.IsHq,
                    b.IsActive,
                    b.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (branch == null)
                return NotFound("Branch not found.");

            return Ok(branch);
        }

        [HttpPost]
        public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized("CompanyNmlsNumber claim is required.");

            var branch = new Branch
            {
                CompanyNmlsNumber = companyNmlsNumber,
                Name = request.Name,
                BranchNmlsNumber = request.BranchNmlsNumber,
                StreetAddress = request.StreetAddress,
                City = request.City,
                State = request.State,
                ZipCode = request.ZipCode,
                IsHq = request.IsHq,
                IsActive = true
            };

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Branch created successfully.",
                branch.Id,
                branch.CompanyNmlsNumber,
                branch.Name,
                branch.BranchNmlsNumber,
                branch.StreetAddress,
                branch.City,
                branch.State,
                branch.ZipCode,
                branch.IsHq,
                branch.IsActive,
                branch.CreatedAt
            });
        }

        [HttpPut("{branchId}")]
        public async Task<IActionResult> UpdateBranch(int branchId, [FromBody] UpdateBranchRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized("CompanyNmlsNumber claim is required.");

            var branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.Id == branchId && b.CompanyNmlsNumber == companyNmlsNumber);

            if (branch == null)
                return NotFound("Branch not found.");

            branch.Name = request.Name;
            branch.BranchNmlsNumber = request.BranchNmlsNumber;
            branch.StreetAddress = request.StreetAddress;
            branch.City = request.City;
            branch.State = request.State;
            branch.ZipCode = request.ZipCode;
            branch.IsHq = request.IsHq;
            branch.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Branch updated successfully.",
                branch.Id,
                branch.CompanyNmlsNumber,
                branch.Name,
                branch.BranchNmlsNumber,
                branch.StreetAddress,
                branch.City,
                branch.State,
                branch.ZipCode,
                branch.IsHq,
                branch.IsActive,
                branch.CreatedAt
            });
        }

        [HttpDelete("{branchId}")]
        public async Task<IActionResult> DeleteBranch(int branchId)
        {
            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized("CompanyNmlsNumber claim is required.");

            var branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.Id == branchId && b.CompanyNmlsNumber == companyNmlsNumber);

            if (branch == null)
                return NotFound("Branch not found.");

            _context.Branches.Remove(branch);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Branch deleted successfully." });
        }
    }
}