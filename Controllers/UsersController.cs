using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;
using PrestexaAPI.Services;
using System.Security.Claims;
using System.Text.Json;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private const long MaxProfilePhotoSizeBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedProfilePhotoContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

        private static readonly HashSet<string> AllowedProfilePhotoExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            AppDbContext context,
            ICurrentUserService currentUserService,
            IConfiguration configuration,
            ILogger<UsersController> logger)
        {
            _context = context;
            _currentUserService = currentUserService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            if (TryBuildCurrentUserClaimError(out var claimError))
                return claimError;

            var currentUser = await LoadCurrentUserAsync(asNoTracking: true);

            if (currentUser == null)
                return NotFound("Authenticated user was not found.");

            var roleMap = await GetActiveRolesByUserIdsAsync([currentUser.Id]);

            return Ok(MapToCurrentUserProfileResponse(currentUser, roleMap));
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateCurrentUserProfile([FromBody] UpdateCurrentUserProfileRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (TryBuildCurrentUserClaimError(out var claimError))
                return claimError;

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
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var roleMap = await GetActiveRolesByUserIdsAsync([user.Id]);

            return Ok(MapToCurrentUserProfileResponse(user, roleMap));
        }

        [HttpPost("me/photo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCurrentUserPhoto(IFormFile file)
        {
            if (TryBuildCurrentUserClaimError(out var claimError))
                return claimError;

            var user = await LoadCurrentUserAsync(asNoTracking: false);

            if (user == null)
                return NotFound("Authenticated user was not found.");

            return await SaveProfilePhotoAsync(
                user,
                file,
                "Profile Photo Uploaded");
        }

        [HttpDelete("me/photo")]
        public async Task<IActionResult> DeleteCurrentUserPhoto()
        {
            if (TryBuildCurrentUserClaimError(out var claimError))
                return claimError;

            var user = await LoadCurrentUserAsync(asNoTracking: false);

            if (user == null)
                return NotFound("Authenticated user was not found.");

            return await RemoveProfilePhotoAsync(
                user,
                "Profile Photo Removed");
        }

        [HttpGet("me/photo")]
        public async Task<IActionResult> GetCurrentUserPhoto()
        {
            if (TryBuildCurrentUserClaimError(out var claimError))
                return claimError;

            var user = await LoadCurrentUserAsync(asNoTracking: true);

            if (user == null)
                return NotFound("Authenticated user was not found.");

            return await StreamProfilePhotoAsync(user);
        }

        [HttpPost("{userId}/photo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadUserPhoto(int userId, IFormFile file)
        {
            if (TryBuildCurrentUserClaimError(out var claimError))
                return claimError;

            if (!IsPhotoAdminRole(GetCurrentRole()))
                return Forbid();

            var user = await LoadAccessibleUserForPhotoAsync(userId);

            if (user == null)
                return NotFound("User not found.");

            return await SaveProfilePhotoAsync(
                user,
                file,
                "Admin Updated User Profile Photo",
                userId);
        }

        [HttpDelete("{userId}/photo")]
        public async Task<IActionResult> DeleteUserPhoto(int userId)
        {
            if (TryBuildCurrentUserClaimError(out var claimError))
                return claimError;

            if (!IsPhotoAdminRole(GetCurrentRole()))
                return Forbid();

            var user = await LoadAccessibleUserForPhotoAsync(userId);

            if (user == null)
                return NotFound("User not found.");

            return await RemoveProfilePhotoAsync(
                user,
                "Admin Removed User Profile Photo",
                userId);
        }

        [HttpGet("{userId}/photo")]
        public async Task<IActionResult> GetUserPhoto(int userId)
        {
            if (TryBuildCurrentUserClaimError(out var claimError))
                return claimError;

            if (_currentUserService.UserId != userId && !_currentUserService.IsSuperAdmin && !IsPhotoAdminRole(GetCurrentRole()))
                return Forbid();

            var user = await LoadAccessibleUserForPhotoAsync(userId);

            if (user == null)
                return NotFound("User not found.");

            return await StreamProfilePhotoAsync(user);
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var companyNmlsNumber = GetCompanyNmlsNumber();
            var role = GetCurrentRole();

            if (string.IsNullOrWhiteSpace(companyNmlsNumber) && !IsSuperAdmin(role))
                return Unauthorized("CompanyNmlsNumber claim is required.");

            var query = _context.Users
                .Include(u => u.ProfilePhotoAsset)
                .AsQueryable();

            if (!IsSuperAdmin(role))
                query = query.Where(u => u.CompanyNmlsNumber == companyNmlsNumber);

            var users = await query.ToListAsync();
            var roleMap = await GetActiveRolesByUserIdsAsync(users.Select(x => x.Id));

            return Ok(users.Select(user => BuildUserResponse(user, roleMap)));
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(int userId)
        {
            var user = await BuildAccessibleUsersQuery()
                .Include(u => u.ProfilePhotoAsset)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found.");

            var roleMap = await GetActiveRolesByUserIdsAsync([user.Id]);

            return Ok(BuildUserResponse(user, roleMap));
        }

        [HttpGet("{userId}/roles/history")]
        public async Task<IActionResult> GetUserRoleHistory(int userId)
        {
            if (!IsUserRoleHistoryAdminRole(GetCurrentRole()))
                return Forbid();

            var user = await BuildAccessibleUsersQuery()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found.");

            var history = await _context.UserRoles
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.AssignedAtUtc)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            var activeRoles = history
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.RoleName)
                .Select(x => x.RoleName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Ok(new
            {
                userId = user.Id,
                primaryRole = user.Role,
                activeRoles,
                history = history.Select(x => new
                {
                    id = x.Id,
                    roleName = x.RoleName,
                    isPrimary = x.IsPrimary,
                    isActive = x.IsActive,
                    assignedByUserId = x.AssignedByUserId,
                    assignedAtUtc = x.AssignedAtUtc,
                    deactivatedByUserId = x.DeactivatedByUserId,
                    deactivatedAtUtc = x.DeactivatedAtUtc,
                    changeReason = x.ChangeReason
                })
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryResolveRequestedRoles(request.PrimaryRole, request.Role, request.Roles, out var primaryRole, out var roleNames, out var roleError))
                return BadRequest(new { error = roleError });

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

            await EnsureSystemRoleExistsAsync(companyNmlsNumber, primaryRole);

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
                Role = primaryRole,
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

            await SyncUserRolesAsync(user, primaryRole, roleNames, request.RoleChangeReason);
            await _context.SaveChangesAsync();

            return await GetUser(user.Id);
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryResolveRequestedRoles(request.PrimaryRole, request.Role, request.Roles, out var primaryRole, out var roleNames, out var roleError))
                return BadRequest(new { error = roleError });

            var user = await BuildAccessibleUsersQuery()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User not found.");

            var normalizedEmail = NormalizeEmail(request.Email);

            var emailInUse = await _context.Users
                .AnyAsync(u => u.Id != userId && u.Email.ToLower() == normalizedEmail);

            if (emailInUse)
                return Conflict("Email already exists.");

            await EnsureSystemRoleExistsAsync(user.CompanyNmlsNumber, primaryRole);

            var currentRole = user.Role;
            var roleChanged = !string.Equals(currentRole, primaryRole, StringComparison.Ordinal);

            user.FirstName = request.FirstName.Trim();
            user.MiddleName = string.IsNullOrWhiteSpace(request.MiddleName)
                ? null
                : request.MiddleName.Trim();
            user.LastName = request.LastName.Trim();
            user.Email = normalizedEmail;
            user.PhoneNumber = request.PhoneNumber.Trim();
            user.Role = primaryRole;
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
                        r.Name == primaryRole);

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

            await SyncUserRolesAsync(user, primaryRole, roleNames, request.RoleChangeReason);

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

            var query = _context.Users
                .Include(u => u.Company)
                .Include(u => u.ProfilePhotoAsset)
                .AsQueryable();

            if (asNoTracking)
                query = query.AsNoTracking();

            if (!_currentUserService.IsSuperAdmin)
            {
                query = query.Where(u => u.CompanyNmlsNumber == _currentUserService.CompanyNmlsNumber);
            }

            return await query.FirstOrDefaultAsync(u => u.Id == currentUserId);
        }

        private async Task<User?> LoadAccessibleUserForPhotoAsync(int userId)
        {
            return await BuildAccessibleUsersQuery()
                .Include(u => u.Company)
                .Include(u => u.ProfilePhotoAsset)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        private static bool IsPhotoAdminRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            return string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Company Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Associate Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Branch Admin", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUserRoleHistoryAdminRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            return string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Company Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Associate Admin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Branch Admin", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<IActionResult> SaveProfilePhotoAsync(
            User user,
            IFormFile? file,
            string auditAction,
            int? responseUserId = null)
        {
            var validationError = ValidateProfilePhotoFile(file);

            if (validationError != null)
                return validationError;

            var normalizedCompanyNmlsNumber = user.CompanyNmlsNumber.Trim();
            var extension = Path.GetExtension(file!.FileName).ToLowerInvariant();
            var storageRoot = ResolveStorageRoot();
            var relativeFolder = Path.Combine("companies", normalizedCompanyNmlsNumber, "users", user.Id.ToString(), "profile-photos");
            var physicalFolder = Path.Combine(storageRoot, relativeFolder);

            Directory.CreateDirectory(physicalFolder);

            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(physicalFolder, storedFileName);

            await using (var stream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            var mediaAsset = new MediaAsset
            {
                StoragePath = Path.Combine(relativeFolder, storedFileName).Replace('\\', '/'),
                ContentType = file.ContentType,
                FileName = Path.GetFileName(file.FileName),
                FileSizeBytes = file.Length,
                VisibilityType = MediaAssetVisibilityType.Public,
                Category = MediaAssetCategory.UserProfilePhoto,
                UploadedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            _context.MediaAssets.Add(mediaAsset);
            await _context.SaveChangesAsync();

            var oldAsset = user.ProfilePhotoAsset;
            var oldAssetId = user.ProfilePhotoAssetId;

            user.ProfilePhotoAssetId = mediaAsset.Id;
            user.ProfilePhotoAsset = mediaAsset;
            user.ProfilePhotoPath = null;
            user.UpdatedAt = DateTime.UtcNow;

            await AddProfilePhotoAuditRecordAsync(
                auditAction,
                user,
                oldAssetId,
                mediaAsset.Id);

            await _context.SaveChangesAsync();

            if (oldAsset != null && oldAsset.Id != mediaAsset.Id)
            {
                oldAsset.IsActive = false;
                await _context.SaveChangesAsync();
            }

            return Ok(BuildProfilePhotoMutationResponse(user, mediaAsset, responseUserId));
        }

        private async Task<IActionResult> RemoveProfilePhotoAsync(
            User user,
            string auditAction,
            int? responseUserId = null)
        {
            var oldAsset = user.ProfilePhotoAsset;
            var oldAssetId = user.ProfilePhotoAssetId;

            if (oldAsset == null && !oldAssetId.HasValue)
            {
                return Ok(BuildProfilePhotoMutationResponse(user, null, responseUserId));
            }

            user.ProfilePhotoAssetId = null;
            user.ProfilePhotoAsset = null;
            user.ProfilePhotoPath = null;
            user.UpdatedAt = DateTime.UtcNow;

            await AddProfilePhotoAuditRecordAsync(
                auditAction,
                user,
                oldAssetId,
                null);

            await _context.SaveChangesAsync();

            if (oldAsset != null && oldAsset.IsActive)
            {
                oldAsset.IsActive = false;
                await _context.SaveChangesAsync();
            }

            return Ok(BuildProfilePhotoMutationResponse(user, null, responseUserId));
        }

        private IActionResult? ValidateProfilePhotoFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Profile photo must be a JPG, PNG, or WEBP image." });

            if (file.Length > MaxProfilePhotoSizeBytes)
                return BadRequest(new { error = "Profile photo must be 5 MB or smaller." });

            var extension = Path.GetExtension(file.FileName);

            if (string.IsNullOrWhiteSpace(extension) || !AllowedProfilePhotoExtensions.Contains(extension))
                return BadRequest(new { error = "Profile photo must be a JPG, PNG, or WEBP image." });

            if (!AllowedProfilePhotoContentTypes.Contains(file.ContentType))
                return BadRequest(new { error = "Profile photo must be a JPG, PNG, or WEBP image." });

            return null;
        }

        private string ResolveStorageRoot()
        {
            var configuredRoot = _configuration["Storage:RootPath"];

            if (string.IsNullOrWhiteSpace(configuredRoot))
            {
                configuredRoot = Directory.Exists("/app/storage")
                    ? "/app/storage"
                    : Path.Combine(Directory.GetCurrentDirectory(), "storage");
            }

            return configuredRoot;
        }

        private async Task AddProfilePhotoAuditRecordAsync(
            string action,
            User targetUser,
            int? oldAssetId,
            int? newAssetId)
        {
            var companyId = targetUser.Company?.Id;

            if (!companyId.HasValue)
            {
                companyId = await _context.Companies
                    .Where(c => c.NmlsNumber == targetUser.CompanyNmlsNumber)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();
            }

            if (!companyId.HasValue || companyId.Value <= 0)
                throw new InvalidOperationException("Company could not be resolved for profile photo audit.");

            _context.OrganizationAuditRecords.Add(new OrganizationAuditRecord
            {
                OrganizationId = companyId.Value,
                CompanyNmlsNumber = targetUser.CompanyNmlsNumber,
                Action = action,
                FieldName = "ProfilePhotoAssetId",
                OldValue = oldAssetId?.ToString(),
                NewValue = newAssetId?.ToString(),
                TargetUserId = targetUser.Id,
                OldAssetId = oldAssetId,
                NewAssetId = newAssetId,
                ChangedByUserId = _currentUserService.UserId,
                ChangedAtUtc = DateTime.UtcNow
            });
        }

        private async Task<IActionResult> StreamProfilePhotoAsync(User user)
        {
            if (!user.ProfilePhotoAssetId.HasValue)
                return NotFound();

            var asset = user.ProfilePhotoAsset;

            if (asset == null || asset.Id != user.ProfilePhotoAssetId.Value)
            {
                asset = await _context.MediaAssets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == user.ProfilePhotoAssetId.Value);
            }

            if (asset == null || !asset.IsActive)
                return NotFound();

            if (asset.Category != MediaAssetCategory.UserProfilePhoto)
                return NotFound();

            var storageRoot = ResolveStorageRoot();
            var physicalPath = Path.Combine(storageRoot, asset.StoragePath);

            if (!System.IO.File.Exists(physicalPath))
            {
                _logger.LogWarning(
                    "Profile photo file missing for MediaAsset {MediaAssetId}",
                    asset.Id);

                return NotFound();
            }

            var stream = new FileStream(
                physicalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            return File(
                stream,
                string.IsNullOrWhiteSpace(asset.ContentType) ? "application/octet-stream" : asset.ContentType,
                enableRangeProcessing: true);
        }

        private static object BuildProfilePhotoMutationResponse(User user, MediaAsset? asset, int? responseUserId)
        {
            var response = new Dictionary<string, object?>
            {
                ["profilePhotoUrl"] = BuildProfilePhotoUrl(asset),
                ["profilePhotoAssetId"] = asset?.Id
            };

            if (responseUserId.HasValue)
                response["userId"] = responseUserId.Value;

            return response;
        }

        private static string? BuildProfilePhotoUrl(MediaAsset? asset)
        {
            if (asset == null || !asset.IsActive)
                return null;

            if (asset.VisibilityType != MediaAssetVisibilityType.Public)
                return null;

            return $"https://api.prestexa.com/api/media/asset/{asset.PublicId}";
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

        private static object MapToCurrentUserProfileResponse(
            User user,
            IReadOnlyDictionary<int, List<string>> roleMap)
        {
            return BuildUserResponse(user, roleMap);
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static object BuildUserResponse(
            User user,
            IReadOnlyDictionary<int, List<string>> roleMap)
        {
            var fullName = string.Join(
                " ",
                new[]
                {
                    user.FirstName,
                    user.MiddleName,
                    user.LastName
                }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()));

            if (!roleMap.TryGetValue(user.Id, out var roles))
                roles = [user.Role];

            return new
            {
                user.Id,
                user.CompanyNmlsNumber,
                user.FirstName,
                user.MiddleName,
                user.LastName,
                FullName = fullName,
                user.Email,
                user.PhoneNumber,
                user.UserNmlsNumber,
                NmlsId = user.UserNmlsNumber,
                user.OfficePhone,
                user.OfficePhoneExtension,
                user.MobilePhone,
                user.ClientFacingTitle,
                PrimaryRole = user.Role,
                user.Role,
                Roles = roles,
                user.SeatType,
                user.Status,
                user.StartDate,
                user.LastLoginAt,
                user.CreatedAt,
                user.UpdatedAt,
                user.TwoFactorEnabled,
                ProfilePhotoAssetId = user.ProfilePhotoAssetId,
                ProfilePhotoUrl = BuildProfilePhotoUrl(user.ProfilePhotoAsset)
            };
        }

        private async Task<IReadOnlyDictionary<int, List<string>>> GetActiveRolesByUserIdsAsync(IEnumerable<int> userIds)
        {
            var ids = userIds
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new Dictionary<int, List<string>>();

            var activeRoles = await _context.UserRoles
                .AsNoTracking()
                .Where(x => x.IsActive && ids.Contains(x.UserId))
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.RoleName)
                .Select(x => new { x.UserId, x.RoleName, x.IsPrimary })
                .ToListAsync();

            return activeRoles
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .Select(x => x.RoleName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList());
        }

        private bool TryResolveRequestedRoles(
            string? requestPrimaryRole,
            string? requestRole,
            List<string>? requestRoles,
            out string primaryRole,
            out List<string> roles,
            out string error)
        {
            primaryRole = string.Empty;
            roles = [];
            error = string.Empty;

            var requestedPrimary = string.IsNullOrWhiteSpace(requestPrimaryRole)
                ? requestRole
                : requestPrimaryRole;

            if (string.IsNullOrWhiteSpace(requestedPrimary))
            {
                error = "primaryRole is required.";
                return false;
            }

            if (!RoleCatalog.IsManagedRole(requestedPrimary))
            {
                error = "Invalid primaryRole. Must be one of the approved Prestexa system roles.";
                return false;
            }

            primaryRole = RoleCatalog.Normalize(requestedPrimary);

            var normalizedRoles = new List<string>();
            if (requestRoles != null && requestRoles.Count > 0)
            {
                foreach (var role in requestRoles)
                {
                    if (!RoleCatalog.IsManagedRole(role))
                    {
                        error = "Invalid roles entry. Must be one of the approved Prestexa system roles.";
                        return false;
                    }

                    normalizedRoles.Add(RoleCatalog.Normalize(role));
                }
            }
            else
            {
                normalizedRoles.Add(primaryRole);
            }

            var distinctRoles = normalizedRoles
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (distinctRoles.Count != normalizedRoles.Count)
            {
                error = "Duplicate roles are not allowed.";
                return false;
            }

            if (!distinctRoles.Contains(primaryRole, StringComparer.OrdinalIgnoreCase))
            {
                error = "roles must include primaryRole.";
                return false;
            }

            var normalizedPrimaryRole = primaryRole;

            roles = distinctRoles
                .OrderByDescending(x => string.Equals(x, normalizedPrimaryRole, StringComparison.OrdinalIgnoreCase))
                .ThenBy(x => x)
                .ToList();

            return true;
        }

        private async Task SyncUserRolesAsync(
            User user,
            string primaryRole,
            IReadOnlyCollection<string> roles,
            string? reason)
        {
            var now = DateTime.UtcNow;
            var changedByUserId = _currentUserService.UserId;
            var normalizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

            var existing = await _context.UserRoles
                .Where(x => x.UserId == user.Id)
                .ToListAsync();

            var organizationId = await ResolveOrganizationIdAsync(user.CompanyNmlsNumber);

            var oldPrimaryRole = existing
                .FirstOrDefault(x => x.IsActive && x.IsPrimary)
                ?.RoleName;

            var oldRoles = existing
                .Where(x => x.IsActive)
                .Select(x => x.RoleName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var existingActive = existing
                .Where(x => x.IsActive)
                .ToList();

                        await _context.Database.ExecuteSqlInterpolatedAsync($@"
                                UPDATE ""UserRoles""
                                SET ""IsPrimary"" = FALSE
                                WHERE ""UserId"" = {user.Id}
                                    AND ""IsPrimary"" = TRUE
                                    AND ""IsActive"" = TRUE");

            var activeRolesAfter = existingActive
                .ToList();

            foreach (var row in activeRolesAfter)
                row.IsPrimary = false;

            var deactivatedRoleNames = new List<string>();
            var assignedRoleNames = new List<string>();

            foreach (var row in existingActive)
            {
                if (roles.Contains(row.RoleName, StringComparer.OrdinalIgnoreCase))
                    continue;

                row.IsActive = false;
                row.IsPrimary = false;
                row.DeactivatedByUserId = changedByUserId;
                row.DeactivatedAtUtc = now;
                row.ChangeReason = normalizedReason;
                deactivatedRoleNames.Add(row.RoleName);
                activeRolesAfter.Remove(row);

                AddRoleAuditEvent(
                    organizationId,
                    user,
                    "UserRoleDeactivated",
                    oldPrimaryRole,
                    primaryRole,
                    oldRoles,
                    roles,
                    null,
                    row.RoleName,
                    now,
                    changedByUserId,
                    normalizedReason);
            }

            foreach (var roleName in roles)
            {
                var activeRow = existing.FirstOrDefault(x =>
                    x.IsActive &&
                    string.Equals(x.RoleName, roleName, StringComparison.OrdinalIgnoreCase));

                if (activeRow != null)
                    continue;

                var newUserRole = new UserRole
                {
                    UserId = user.Id,
                    RoleName = roleName,
                    IsPrimary = false,
                    AssignedByUserId = changedByUserId,
                    AssignedAtUtc = now,
                    DeactivatedByUserId = null,
                    DeactivatedAtUtc = null,
                    ChangeReason = normalizedReason,
                    IsActive = true
                };

                _context.UserRoles.Add(newUserRole);
                activeRolesAfter.Add(newUserRole);

                assignedRoleNames.Add(roleName);

                AddRoleAuditEvent(
                    organizationId,
                    user,
                    "UserRoleAssigned",
                    oldPrimaryRole,
                    primaryRole,
                    oldRoles,
                    roles,
                    roleName,
                    null,
                    now,
                    changedByUserId,
                    normalizedReason);
            }

            foreach (var row in activeRolesAfter)
            {
                row.IsPrimary = string.Equals(row.RoleName, primaryRole, StringComparison.OrdinalIgnoreCase);
                row.DeactivatedByUserId = null;
                row.DeactivatedAtUtc = null;
            }

            var newPrimaryRole = primaryRole;
            var newRoles = roles
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            if (!string.Equals(oldPrimaryRole, newPrimaryRole, StringComparison.OrdinalIgnoreCase))
            {
                AddRoleAuditEvent(
                    organizationId,
                    user,
                    "UserPrimaryRoleChanged",
                    oldPrimaryRole,
                    newPrimaryRole,
                    oldRoles,
                    newRoles,
                    null,
                    null,
                    now,
                    changedByUserId,
                    normalizedReason);
            }

            var rolesChanged = oldRoles.Count != newRoles.Count
                || oldRoles.Except(newRoles, StringComparer.OrdinalIgnoreCase).Any()
                || newRoles.Except(oldRoles, StringComparer.OrdinalIgnoreCase).Any();

            if (rolesChanged)
            {
                AddRoleAuditEvent(
                    organizationId,
                    user,
                    "UserRolesReplaced",
                    oldPrimaryRole,
                    newPrimaryRole,
                    oldRoles,
                    newRoles,
                    assignedRoleNames.Count == 0 ? null : string.Join(",", assignedRoleNames),
                    deactivatedRoleNames.Count == 0 ? null : string.Join(",", deactivatedRoleNames),
                    now,
                    changedByUserId,
                    normalizedReason);
            }
        }

        private async Task<int> ResolveOrganizationIdAsync(string companyNmlsNumber)
        {
            var organizationId = await _context.Companies
                .Where(x => x.NmlsNumber == companyNmlsNumber)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (organizationId <= 0)
                throw new InvalidOperationException("Company could not be resolved for role audit.");

            return organizationId;
        }

        private void AddRoleAuditEvent(
            int organizationId,
            User user,
            string action,
            string? oldPrimaryRole,
            string? newPrimaryRole,
            IReadOnlyCollection<string> oldRoles,
            IReadOnlyCollection<string> newRoles,
            string? assignedRole,
            string? deactivatedRole,
            DateTime changedAtUtc,
            int? changedByUserId,
            string? reason)
        {
            var payload = JsonSerializer.Serialize(new
            {
                targetUserId = user.Id,
                changedByUserId,
                companyNmlsNumber = user.CompanyNmlsNumber,
                oldPrimaryRole,
                newPrimaryRole,
                oldRoles,
                newRoles,
                assignedRole,
                deactivatedRole,
                changedAtUtc,
                reason
            });

            var safePayload = payload.Length <= 1000 ? payload : payload[..1000];

            _context.OrganizationAuditRecords.Add(new OrganizationAuditRecord
            {
                OrganizationId = organizationId,
                CompanyNmlsNumber = user.CompanyNmlsNumber,
                Action = action,
                FieldName = "UserRoles",
                OldValue = oldRoles.Count == 0 ? null : string.Join(",", oldRoles),
                NewValue = safePayload,
                TargetUserId = user.Id,
                ChangedByUserId = changedByUserId,
                ChangedAtUtc = changedAtUtc
            });
        }
    }
}