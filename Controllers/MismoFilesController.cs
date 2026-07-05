using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class MismoFilesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const long MaxMismoFileSize = 25 * 1024 * 1024; // 25MB

        public MismoFilesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("loans/{loanNumber}/mismo")]
        public async Task<IActionResult> UploadMismoFile(
            string loanNumber,
            IFormFile file,
            string? mismoVersion)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No MISMO file uploaded.");

            if (file.Length > MaxMismoFileSize)
                return BadRequest("Max MISMO file size is 25MB.");

            var originalFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".xml",
                ".mismo"
            };

            var allowedContentTypes = new[]
            {
                "application/xml",
                "text/xml",
                "application/octet-stream"
            };

            if (!allowedExtensions.Contains(extension))
                return BadRequest("Invalid MISMO file extension. Only .xml or .mismo files are allowed.");

            if (!allowedContentTypes.Contains(file.ContentType))
                return BadRequest("Invalid MISMO file content type.");

            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var loan = await BuildLoanAccessQuery(userId, companyNmlsNumber, role)
                .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber);

            if (loan == null)
                return NotFound("Loan not found.");

            var companyFolderName = SanitizeFolderName(loan.CompanyNmlsNumber);

            var mismoFolder = Path.Combine(
                "storage",
                "companies",
                companyFolderName,
                "loans",
                loan.LoanNumber,
                "mismo"
            );

            Directory.CreateDirectory(mismoFolder);

            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(mismoFolder, storedFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = Path.Combine(
                "companies",
                companyFolderName,
                "loans",
                loan.LoanNumber,
                "mismo",
                storedFileName
            );

            var mismoFile = new MismoFile
            {
                CompanyNmlsNumber = loan.CompanyNmlsNumber,
                LoanId = loan.Id,
                UserId = userId,
                FileName = originalFileName,
                FilePath = relativePath,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/xml"
                    : file.ContentType,
                FileSize = new FileInfo(fullPath).Length,
                UploadedAt = DateTime.UtcNow,
                MismoVersion = mismoVersion
            };

            _context.MismoFiles.Add(mismoFile);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "MISMO file uploaded successfully.",
                mismoFile.Id,
                mismoFile.FileName,
                mismoFile.FilePath,
                mismoFile.FileSize,
                mismoFile.ContentType,
                mismoFile.MismoVersion,
                mismoFile.UploadedAt
            });
        }

        [HttpGet("loans/{loanNumber}/mismo")]
        public async Task<IActionResult> GetMismoFiles(string loanNumber)
        {
            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var loan = await BuildLoanAccessQuery(userId, companyNmlsNumber, role)
                .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber);

            if (loan == null)
                return NotFound("Loan not found.");

            var files = await _context.MismoFiles
                .Where(m => m.LoanId == loan.Id)
                .Select(m => new
                {
                    m.Id,
                    m.FileName,
                    m.FilePath,
                    m.ContentType,
                    m.FileSize,
                    m.MismoVersion,
                    m.UploadedAt,
                    m.CompanyNmlsNumber
                })
                .ToListAsync();

            return Ok(files);
        }

        [HttpGet("mismo/{mismoFileId}/download")]
        public async Task<IActionResult> DownloadMismoFile(int mismoFileId)
        {
            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var query = BuildMismoAccessQuery(userId, companyNmlsNumber, role);

            var mismoFile = await query.FirstOrDefaultAsync(m => m.Id == mismoFileId);

            if (mismoFile == null)
                return NotFound("MISMO file not found.");

            var fullPath = Path.Combine("storage", mismoFile.FilePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound("File not found.");

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);

            return File(bytes, mismoFile.ContentType, mismoFile.FileName);
        }

        [HttpDelete("mismo/{mismoFileId}")]
        public async Task<IActionResult> DeleteMismoFile(int mismoFileId)
        {
            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var query = BuildMismoAccessQuery(userId, companyNmlsNumber, role);

            var mismoFile = await query.FirstOrDefaultAsync(m => m.Id == mismoFileId);

            if (mismoFile == null)
                return NotFound("MISMO file not found.");

            var fullPath = Path.Combine("storage", mismoFile.FilePath);

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _context.MismoFiles.Remove(mismoFile);
            await _context.SaveChangesAsync();

            return Ok(new { message = "MISMO file deleted successfully." });
        }

        private IQueryable<Loan> BuildLoanAccessQuery(
            int userId,
            string? companyNmlsNumber,
            string role)
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

        private static string SanitizeFolderName(string value)
        {
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidChar, '_');
            }

            return value.Trim();
        }
    }
}