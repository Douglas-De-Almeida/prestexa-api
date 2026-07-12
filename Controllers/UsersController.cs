using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;
using PrestexaAPI.Services;
using System.Security.Claims;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public UsersController(AppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            if (TryBuildCurrentUserClaimError(out var claimError))
                return claimError;

            var currentUser = await LoadCurrentUserAsync(asNoTracking: true);

            if (currentUser == null)
                return NotFound("Authenticated user was not found.");

            return Ok(MapToCurrentUserProfileResponse(currentUser));
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateCurrentUserProfile([FromBody] UpdateCurrentUserProfileRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (TryBuildCurrentUserClaimError(out var claimError))
                return claimError;

            if (request.ProfilePhotoAssetId.HasValue)
                return BadRequest("profilePhotoAssetId is not supported by this API.");

            var user = await LoadCurrentUserAsync(asNoTracking: false);

            if (user == null)
                return NotFound("Authenticated user was not found.");

            user.FirstName = request.FirstName.Trim();
            user.MiddleName = NormalizeOptional(request.MiddleName);
            user.LastName = request.LastName.Trim();
            user.OfficePhone = NormalizeOptional(request.OfficePhone);
            user.OfficePhoneExtension = NormalizeOptional(request.OfficePhoneExtension);
            user.MobilePhone = NormalizeOptional(request.MobilePhone);
            user.ClientFacingTitle = NormalizeOptional(request.ClientFacingTitle);
            user.ProfilePhotoPath = NormalizeOptional(request.ProfilePhotoUrl);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(MapToCurrentUserProfileResponse(user));
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var companyNmlsNumber = GetCompanyNmlsNumber();
            var role = GetCurrentRole();

            if (string.IsNullOrWhiteSpace(companyNmlsNumber) && !IsSuperAdmin(role))
                return Unauthorized("CompanyNmlsNumber claim is required.");

            var query = _context.Users.AsQueryable();

            if (!IsSuperAdmin(role))
                query = query.Where(u => u.CompanyNmlsNumber == companyNmlsNumber);

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
                    u.UpdatedAt,
                    u.TwoFactorEnabled
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(int userId)
        {
            var user = await BuildAccessibleUsersQuery()
                .Where(u => u.Id == userId)
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
                    u.UpdatedAt,
                    u.TwoFactorEnabled
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound("User not found.");

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!RoleCatalog.IsManagedRole(request.Role))
                return BadRequest(new { error = "Invalid role. Must be one of the approved Prestexa system roles." });

            var companyNmlsNumber = ResolveWriteCompanyNmlsNumber(request.CompanyNmlsNumber);

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized("CompanyNmlsNumber claim is required.");

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.NmlsNumber == companyNmlsNumber);

            if (company == null)
                return BadRequest("Company not found.");

            if (!company.IsActive)
                return BadRequest("Company is inactive.");

            var normalizedEmail = NormalizeEmail(request.Email);

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (existingUser != null)
                return Conflict("User already exists.");

            var normalizedRole = RoleCatalog.Normalize(request.Role);

            await EnsureSystemRoleExistsAsync(companyNmlsNumber, normalizedRole);

            var status = UserStatus.Active;

            if (!string.IsNullOrWhiteSpace(request.Status) &&
                !TryParseUserStatus(request.Status, out status))
            {
                return BadRequest("Invalid user status.");
            }

            var user = new User
            {
                CompanyNmlsNumber = companyNmlsNumber,
                FirstName = request.FirstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(request.MiddleName)
                    ? null
                    : request.MiddleName.Trim(),
                LastName = request.LastName.Trim(),
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber.Trim(),
                Role = normalizedRole,
                UserNmlsNumber = string.IsNullOrWhiteSpace(request.UserNmlsNumber)
                    ? null
                    : request.UserNmlsNumber.Trim(),
                OfficePhone = string.IsNullOrWhiteSpace(request.OfficePhone)
                    ? null
                    : request.OfficePhone.Trim(),
                OfficePhoneExtension = string.IsNullOrWhiteSpace(request.OfficePhoneExtension)
                    ? null
                    : request.OfficePhoneExtension.Trim(),
                MobilePhone = string.IsNullOrWhiteSpace(request.MobilePhone)
                    ? null
                    : request.MobilePhone.Trim(),
                ClientFacingTitle = string.IsNullOrWhiteSpace(request.ClientFacingTitle)
                    ? null
                    : request.ClientFacingTitle.Trim(),
                SeatType = SeatType.Originator,
                Status = status,
                TwoFactorEnabled = false,
                TwoFactorSecret = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return await GetUser(user.Id);
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!RoleCatalog.IsManagedRole(request.Role))
                return BadRequest(new { error = "Invalid role. Must be one of the approved Prestexa system roles." });

            var user = await BuildAccessibleUsersQuery()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found.");

            var normalizedEmail = NormalizeEmail(request.Email);

            var emailInUse = await _context.Users
                .AnyAsync(u => u.Id != userId && u.Email.ToLower() == normalizedEmail);

            if (emailInUse)
                return Conflict("Email already exists.");

            var normalizedRole = RoleCatalog.Normalize(request.Role);

            await EnsureSystemRoleExistsAsync(user.CompanyNmlsNumber, normalizedRole);

            var currentRole = user.Role;
            var roleChanged = !string.Equals(currentRole, normalizedRole, StringComparison.Ordinal);

            user.FirstName = request.FirstName.Trim();
            user.MiddleName = string.IsNullOrWhiteSpace(request.MiddleName)
                ? null
                : request.MiddleName.Trim();
            user.LastName = request.LastName.Trim();
            user.Email = normalizedEmail;
            user.PhoneNumber = request.PhoneNumber.Trim();
            user.Role = normalizedRole;
            user.UserNmlsNumber = string.IsNullOrWhiteSpace(request.UserNmlsNumber)
                ? null
                : request.UserNmlsNumber.Trim();
            user.OfficePhone = string.IsNullOrWhiteSpace(request.OfficePhone)
                ? null
                : request.OfficePhone.Trim();
            user.OfficePhoneExtension = string.IsNullOrWhiteSpace(request.OfficePhoneExtension)
                ? null
                : request.OfficePhoneExtension.Trim();
            user.MobilePhone = string.IsNullOrWhiteSpace(request.MobilePhone)
                ? null
                : request.MobilePhone.Trim();
            user.ClientFacingTitle = string.IsNullOrWhiteSpace(request.ClientFacingTitle)
                ? null
                : request.ClientFacingTitle.Trim();
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (!TryParseUserStatus(request.Status, out var parsedStatus))
                    return BadRequest("Invalid user status.");

                user.Status = parsedStatus;
            }
            user.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            if (roleChanged)
            {
                var roleEntity = await _context.Roles
                    .FirstOrDefaultAsync(r =>
                        r.CompanyNmlsNumber == user.CompanyNmlsNumber &&
                        r.Name == normalizedRole);

                if (roleEntity == null)
                    return BadRequest("Role catalog entry could not be resolved.");

                var branchRoles = await _context.UserBranchRoles
                    .Where(ubr => ubr.UserId == user.Id && ubr.CompanyNmlsNumber == user.CompanyNmlsNumber)
                    .ToListAsync();

                foreach (var branchRole in branchRoles)
                {
                    branchRole.RoleId = roleEntity.Id;
                }
            }

            await _context.SaveChangesAsync();

            return await GetUser(user.Id);
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var user = await BuildAccessibleUsersQuery()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found.");

            user.Status = UserStatus.Inactive;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User disabled successfully.",
                user.Id,
                user.Email,
                user.Status
            });
        }

        [HttpGet("{userId}/branches")]
        public async Task<IActionResult> GetUserBranches(int userId)
        {
            var user = await BuildAccessibleUsersQuery()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found.");

            var assignments = await _context.UserBranchRoles
                .AsNoTracking()
                .Where(ubr => ubr.UserId == user.Id && ubr.CompanyNmlsNumber == user.CompanyNmlsNumber)
                .Select(ubr => new
                {
                    ubr.Id,
                    ubr.CompanyNmlsNumber,
                    ubr.UserId,
                    ubr.BranchId,
                    BranchName = ubr.Branch.Name,
                    BranchNmlsNumber = ubr.Branch.BranchNmlsNumber,
                    ubr.RoleId,
                    RoleName = ubr.Role.Name,
                    ubr.IsDefaultBranch,
                    ubr.CreatedAt
                })
                .OrderByDescending(x => x.IsDefaultBranch)
                .ThenBy(x => x.BranchName)
                .ToListAsync();

            return Ok(assignments);
        }

        [HttpPut("{userId}/branches")]
        public async Task<IActionResult> UpdateUserBranches(
            int userId,
            [FromBody] UpdateUserBranchesRequest request)
        {
            var user = await BuildAccessibleUsersQuery()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found.");

            if (!RoleCatalog.IsManagedRole(user.Role))
                return BadRequest("Branch assignment is only supported for managed employee roles.");

            var branchIds = request.BranchIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (request.DefaultBranchId.HasValue && !branchIds.Contains(request.DefaultBranchId.Value))
                return BadRequest("DefaultBranchId must be included in BranchIds.");

            var branches = await _context.Branches
                .Where(b => b.CompanyNmlsNumber == user.CompanyNmlsNumber && branchIds.Contains(b.Id))
                .ToListAsync();

            if (branches.Count != branchIds.Count)
                return BadRequest("One or more branch ids are invalid for this company.");

            var roleEntity = await _context.Roles
                .FirstOrDefaultAsync(r =>
                    r.CompanyNmlsNumber == user.CompanyNmlsNumber &&
                    r.Name == user.Role);

            if (roleEntity == null)
            {
                roleEntity = new Role
                {
                    CompanyNmlsNumber = user.CompanyNmlsNumber,
                    Name = user.Role,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Roles.Add(roleEntity);
                await _context.SaveChangesAsync();
            }

            var currentAssignments = await _context.UserBranchRoles
                .Where(ubr => ubr.UserId == user.Id && ubr.CompanyNmlsNumber == user.CompanyNmlsNumber)
                .ToListAsync();

            _context.UserBranchRoles.RemoveRange(currentAssignments);

            var defaultBranchId = request.DefaultBranchId ?? branchIds.FirstOrDefault();

            foreach (var branch in branches)
            {
                _context.UserBranchRoles.Add(new UserBranchRole
                {
                    CompanyNmlsNumber = user.CompanyNmlsNumber,
                    UserId = user.Id,
                    BranchId = branch.Id,
                    RoleId = roleEntity.Id,
                    IsDefaultBranch = branch.Id == defaultBranchId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return await GetUserBranches(userId);
        }

        private IQueryable<User> BuildAccessibleUsersQuery()
        {
            var role = GetCurrentRole();
            var query = _context.Users.AsQueryable();

            if (!IsSuperAdmin(role))
            {
                var companyNmlsNumber = GetCompanyNmlsNumber();

                if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                    return query.Where(u => false);

                query = query.Where(u => u.CompanyNmlsNumber == companyNmlsNumber);
            }

            return query;
        }

        private string? ResolveWriteCompanyNmlsNumber(string? requestedCompanyNmlsNumber)
        {
            var role = GetCurrentRole();

            if (IsSuperAdmin(role))
                return string.IsNullOrWhiteSpace(requestedCompanyNmlsNumber)
                    ? GetCompanyNmlsNumber()
                    : requestedCompanyNmlsNumber.Trim();

            return GetCompanyNmlsNumber();
        }

        private async Task EnsureSystemRoleExistsAsync(string companyNmlsNumber, string roleName)
        {
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r =>
                    r.CompanyNmlsNumber == companyNmlsNumber &&
                    r.Name == roleName);

            if (existingRole != null)
                return;

            _context.Roles.Add(new Role
            {
                CompanyNmlsNumber = companyNmlsNumber,
                Name = roleName,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        private static bool TryParseUserStatus(string? value, out UserStatus status)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                status = UserStatus.Active;
                return true;
            }

            return Enum.TryParse(value.Trim(), true, out status);
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private string? GetCompanyNmlsNumber()
        {
            return User.FindFirst("CompanyNmlsNumber")?.Value;
        }

        private string? GetCurrentRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        private static bool IsSuperAdmin(string? role)
        {
            return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<User?> LoadCurrentUserAsync(bool asNoTracking)
        {
            var currentUserId = _currentUserService.UserId!.Value;

            var query = _context.Users.AsQueryable();

            if (asNoTracking)
                query = query.AsNoTracking();

            if (!_currentUserService.IsSuperAdmin)
            {
                query = query.Where(u => u.CompanyNmlsNumber == _currentUserService.CompanyNmlsNumber);
            }

            return await query.FirstOrDefaultAsync(u => u.Id == currentUserId);
        }

        private bool TryBuildCurrentUserClaimError(out IActionResult error)
        {
            if (!_currentUserService.UserId.HasValue)
            {
                error = Unauthorized("UserId claim is required.");
                return true;
            }

            if (!_currentUserService.IsSuperAdmin && string.IsNullOrWhiteSpace(_currentUserService.CompanyNmlsNumber))
            {
                error = Unauthorized("CompanyNmlsNumber claim is required.");
                return true;
            }

            error = null!;
            return false;
        }

        private static object MapToCurrentUserProfileResponse(User user)
        {
            var fullName = string.Join(
                " ",
                new[]
                {
                    user.FirstName,
                    user.MiddleName,
                    user.LastName
                }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));

            return new
            {
                user.Id,
                user.FirstName,
                user.MiddleName,
                user.LastName,
                FullName = fullName,
                user.Email,
                user.Role,
                user.ClientFacingTitle,
                NmlsId = user.UserNmlsNumber,
                user.OfficePhone,
                user.OfficePhoneExtension,
                user.MobilePhone,
                FacebookProfileUrl = string.Empty,
                TwitterHandle = string.Empty,
                LinkedInProfileUrl = string.Empty,
                InstagramHandle = string.Empty,
                ProfilePhotoUrl = string.IsNullOrWhiteSpace(user.ProfilePhotoPath)
                    ? null
                    : user.ProfilePhotoPath,
                user.TwoFactorEnabled,
                Status = user.Status.ToString(),
                user.CompanyNmlsNumber
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}