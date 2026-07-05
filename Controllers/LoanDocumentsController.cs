using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

using iText.Kernel.Pdf;
using iText.Layout;
using iText.IO.Image;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class LoanDocumentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const long MaxUploadSize = 15 * 1024 * 1024;

        public LoanDocumentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("loans/{loanNumber}/documents/{category}")]
        public async Task<IActionResult> UploadLoanDocument(
            string loanNumber,
            string category,
            IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (file.Length > MaxUploadSize)
                return BadRequest("Max upload size is 15MB.");

            if (!TryParseDocumentCategory(category, out var documentCategory))
                return BadRequest("Invalid document category.");

            var originalFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            var allowedTypes = new[]
            {
                "application/pdf",
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/webp",
                "image/tiff"
            };

            var allowedExtensions = new[]
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png",
                ".webp",
                ".tif",
                ".tiff"
            };

            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest("Invalid file type.");

            if (!allowedExtensions.Contains(extension))
                return BadRequest("Invalid file extension.");

            if (file.ContentType == "image/gif" || extension == ".gif")
                return BadRequest("GIF files are not allowed.");

            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var loan = await BuildLoanAccessQuery(userId, companyNmlsNumber, role)
                .Include(l => l.Company)
                .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber);

            if (loan == null)
                return NotFound("Loan not found.");

            var companyFolderName = SanitizeFolderName(loan.CompanyNmlsNumber);
            var categoryFolderName = GetDocumentCategoryFolder(documentCategory);

            var loanFolder = Path.Combine(
                "storage",
                "companies",
                companyFolderName,
                "loans",
                loan.LoanNumber,
                categoryFolderName
            );

            Directory.CreateDirectory(loanFolder);

            var storedFileName = $"{Guid.NewGuid()}.pdf";
            var fullPath = Path.Combine(loanFolder, storedFileName);

            try
            {
                if (file.ContentType == "application/pdf")
                {
                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
                else
                {
                    using var input = file.OpenReadStream();
                    using var image = await Image.LoadAsync(input);

                    image.Mutate(x =>
                    {
                        x.AutoOrient();
                        x.BackgroundColor(Color.White);
                    });

                    using var cleanStream = new MemoryStream();
                    await image.SaveAsJpegAsync(cleanStream);

                    var cleanBytes = cleanStream.ToArray();

                    if (cleanBytes.Length == 0)
                        return BadRequest("Image conversion failed.");

                    using var writer = new PdfWriter(fullPath);
                    using var pdf = new PdfDocument(writer);
                    var document = new Document(pdf);

                    var imageData = ImageDataFactory.Create(cleanBytes);
                    var pdfImage = new iText.Layout.Element.Image(imageData);

                    pdfImage.SetAutoScale(true);
                    document.Add(pdfImage);
                    document.Close();
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing file: {ex.Message}");
            }

            var relativePath = Path.Combine(
                "companies",
                companyFolderName,
                "loans",
                loan.LoanNumber,
                categoryFolderName,
                storedFileName
            );

            var documentEntity = new LoanDocument
            {
                FileName = originalFileName,
                FilePath = relativePath,
                ContentType = "application/pdf",
                FileSize = new FileInfo(fullPath).Length,
                UploadedAt = DateTime.UtcNow,
                Category = documentCategory,
                LoanId = loan.Id,
                UserId = userId,
                CompanyNmlsNumber = loan.CompanyNmlsNumber
            };

            _context.LoanDocuments.Add(documentEntity);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "File uploaded successfully.",
                id = documentEntity.Id,
                fileName = documentEntity.FileName,
                category = documentEntity.Category.ToString(),
                fileSize = documentEntity.FileSize,
                contentType = documentEntity.ContentType,
                uploadedAt = documentEntity.UploadedAt,
                path = documentEntity.FilePath
            });
        }

        [HttpGet("loans/{loanNumber}/documents")]
        public async Task<IActionResult> GetLoanDocuments(string loanNumber, string? category)
        {
            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var loan = await BuildLoanAccessQuery(userId, companyNmlsNumber, role)
                .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber);

            if (loan == null)
                return NotFound("Loan not found.");

            var query = BuildDocumentAccessQuery(userId, companyNmlsNumber, role)
                .Where(d => d.LoanId == loan.Id);

            if (!string.IsNullOrWhiteSpace(category))
            {
                if (!TryParseDocumentCategory(category, out var parsedCategory))
                    return BadRequest("Invalid document category.");

                query = query.Where(d => d.Category == parsedCategory);
            }

            var documents = await query
                .Select(d => new
                {
                    d.Id,
                    d.FileName,
                    d.Category,
                    d.ContentType,
                    d.FileSize,
                    d.UploadedAt,
                    d.FilePath,
                    d.CompanyNmlsNumber,
                    d.UserId
                })
                .ToListAsync();

            return Ok(documents);
        }

        [HttpGet("documents/{documentId}/download")]
        public async Task<IActionResult> DownloadDocument(int documentId)
        {
            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var documentEntity = await BuildDocumentAccessQuery(userId, companyNmlsNumber, role)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (documentEntity == null)
                return NotFound("Document not found.");

            var fullPath = Path.Combine("storage", documentEntity.FilePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound("File not found.");

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);

            return File(bytes, "application/pdf", documentEntity.FileName);
        }

        [HttpDelete("documents/{documentId}")]
        public async Task<IActionResult> DeleteDocument(int documentId)
        {
            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var documentEntity = await BuildDocumentAccessQuery(userId, companyNmlsNumber, role)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (documentEntity == null)
                return NotFound("Document not found.");

            var fullPath = Path.Combine("storage", documentEntity.FilePath);

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _context.LoanDocuments.Remove(documentEntity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Document deleted successfully." });
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

        private IQueryable<LoanDocument> BuildDocumentAccessQuery(
            int userId,
            string? companyNmlsNumber,
            string role)
        {
            var query = _context.LoanDocuments.AsQueryable();

            if (!IsSuperAdmin(role))
            {
                if (string.IsNullOrWhiteSpace(companyNmlsNumber))
                    return query.Where(d => false);

                query = query.Where(d => d.CompanyNmlsNumber == companyNmlsNumber);
            }

            if (IsLoanOfficer(role))
                query = query.Where(d => d.UserId == userId);

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

        private static bool TryParseDocumentCategory(string value, out DocumentStorageCategory category)
        {
            return Enum.TryParse(value, true, out category);
        }

        private static string GetDocumentCategoryFolder(DocumentStorageCategory category)
        {
            return category switch
            {
                DocumentStorageCategory.Documents => "documents",
                DocumentStorageCategory.Disclosures => "disclosures",
                DocumentStorageCategory.Conditions => "conditions",
                DocumentStorageCategory.Credit => "credit",
                DocumentStorageCategory.Aus => "aus",
                DocumentStorageCategory.Pricing => "pricing",
                DocumentStorageCategory.Closing => "closing",
                DocumentStorageCategory.Mismo => "mismo",
                _ => "documents"
            };
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
