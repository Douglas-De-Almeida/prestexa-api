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
    public class RolesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RolesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized();

            var roles = await _context.Roles
                .Where(r => r.CompanyNmlsNumber == companyNmlsNumber)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.CompanyNmlsNumber,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(roles);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized();

            var role = new Role
            {
                CompanyNmlsNumber = companyNmlsNumber,
                Name = request.Name
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return Ok(role);
        }
    }
}