using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.IsNullOrWhiteSpace(companyNmlsNumber) &&
                !string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized("CompanyNmlsNumber claim is required.");
            }

            var query = _context.Users.AsQueryable();

            if (!string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => u.CompanyNmlsNumber == companyNmlsNumber);
            }

            var users = await query
                .Select(u => new
                {
                    u.Id,
                    u.CompanyNmlsNumber,
                    u.FirstName,
                    u.MiddleName,
                    u.LastName,
                    u.Email,
                    u.PhoneNumber,
                    u.UserNmlsNumber,
                    u.OfficePhone,
                    u.OfficePhoneExtension,
                    u.MobilePhone,
                    u.ClientFacingTitle,
                    u.Role,
                    u.SeatType,
                    u.Status,
                    u.StartDate,
                    u.LastLoginAt,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(int userId)
        {
            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            var query = _context.Users.Where(u => u.Id == userId);

            if (!string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                    return Unauthorized("CompanyNmlsNumber claim is required.");

                query = query.Where(u => u.CompanyNmlsNumber == companyNmlsNumber);
            }

            var user = await query
                .Select(u => new
                {
                    u.Id,
                    u.CompanyNmlsNumber,
                    u.FirstName,
                    u.MiddleName,
                    u.LastName,
                    u.Email,
                    u.PhoneNumber,
                    u.UserNmlsNumber,
                    u.OfficePhone,
                    u.OfficePhoneExtension,
                    u.MobilePhone,
                    u.ClientFacingTitle,
                    u.Role,
                    u.SeatType,
                    u.Status,
                    u.StartDate,
                    u.LastLoginAt,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound("User not found.");

            return Ok(user);
        }
    }
}