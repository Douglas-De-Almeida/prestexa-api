using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Services.Mismo;
using System.Security.Claims;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/mismo")]
    [Authorize]
    public class MismoImportController : ControllerBase
    {
        private readonly IMismoImportService _mismoImportService;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        private const long MaxMismoFileSize = 25 * 1024 * 1024;

        public MismoImportController(
            IMismoImportService mismoImportService,
            AppDbContext context,
            IConfiguration configuration)
        {
            _mismoImportService = mismoImportService;
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("import")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportMismo(IFormFile file, int? branchId, bool allowDuplicate = false)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No MISMO file uploaded.");

            if (file.Length > MaxMismoFileSize)
                return BadRequest("Max MISMO file size is 25MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension != ".xml" && extension != ".mismo")
                return BadRequest("Invalid MISMO file extension. Only .xml or .mismo files are allowed.");

            var userIdValue = User.FindFirst("UserId")?.Value;
            var companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;

            if (!int.TryParse(userIdValue, out var userId))
                return Unauthorized("UserId claim is required.");

            if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                return Unauthorized("CompanyNmlsNumber claim is required.");

            await using var stream = file.OpenReadStream();

            try
            {
                var result = await _mismoImportService.ImportAsync(
                    stream,
                    userId,
                    companyNmlsNumber,
                    branchId,
                    sourceMismoFileId: null,
                    allowDuplicate: allowDuplicate,
                    HttpContext.RequestAborted
                );

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("import-from-file/{mismoFileId:int}")]
        public async Task<IActionResult> ImportMismoFromFile(int mismoFileId, int? branchId, bool allowDuplicate = false)
        {
            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var query = BuildMismoAccessQuery(userId, companyNmlsNumber, role);

            var mismoFile = await query.FirstOrDefaultAsync(m => m.Id == mismoFileId);

            if (mismoFile == null)
                return NotFound("MISMO file not found.");

            var storageRoot = _configuration["Storage:RootPath"];

            if (string.IsNullOrWhiteSpace(storageRoot))
            {
                storageRoot = Directory.Exists("/app/storage")
                    ? "/app/storage"
                    : Path.Combine(Directory.GetCurrentDirectory(), "storage");
            }

            var fullPath = Path.Combine(storageRoot, mismoFile.FilePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound("MISMO file content not found in storage.");

            await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            try
            {
                var result = await _mismoImportService.ImportAsync(
                    stream,
                    userId,
                    companyNmlsNumber!,
                    branchId,
                    sourceMismoFileId: mismoFileId,
                    allowDuplicate: allowDuplicate,
                    HttpContext.RequestAborted
                );

                return Ok(new
                {
                    sourceMismoFileId = mismoFileId,
                    sourceMismoFileName = mismoFile.FileName,
                    importResult = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private IQueryable<MismoFile> BuildMismoAccessQuery(
            int userId,
            string? companyNmlsNumber,
            string role)
        {
            var query = _context.MismoFiles.AsQueryable();

            if (!IsSuperAdmin(role))
            {
                if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                    return query.Where(m => false);

                query = query.Where(m => m.CompanyNmlsNumber == companyNmlsNumber);
            }

            if (IsLoanOfficer(role))
                query = query.Where(m => m.UserId == userId);

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
