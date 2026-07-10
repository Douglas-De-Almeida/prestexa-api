using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;
using PrestexaAPI.Models.Responses;
using System.Security.Claims;
using System.Text;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Route("api/loans/{loanNumber}/activities")]
    [Authorize]
    public class LoanActivitiesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const int DefaultPageSize = 50;
        private const int MaxPageSize = 100;
        private const long MaxUploadSize = 15 * 1024 * 1024;

        public LoanActivitiesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetActivities(
            string loanNumber,
            [FromQuery] string? cursor,
            [FromQuery] string? types,
            [FromQuery] string? view = "all",
            [FromQuery] int pageSize = DefaultPageSize)
        {
            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var loan = await BuildLoanAccessQuery(userId, companyNmlsNumber, role)
                .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber);

            if (loan == null)
                return NotFound("Loan not found.");

            var requestedTypes = ParseActivityTypes(types);
            var includeReplies = string.IsNullOrWhiteSpace(view) ||
                                 string.Equals(view, "all", StringComparison.OrdinalIgnoreCase);

            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var baseQuery = _context.LoanActivities
                .AsNoTracking()
                .Where(a => a.LoanId == loan.Id && a.ParentActivityId == null);

            if (requestedTypes.Count > 0)
                baseQuery = baseQuery.Where(a => requestedTypes.Contains(a.ActivityType));

            if (TryDecodeCursor(cursor, out var cursorCreatedAtUtc, out var cursorId))
            {
                baseQuery = baseQuery.Where(a =>
                    a.CreatedAtUtc < cursorCreatedAtUtc ||
                    (a.CreatedAtUtc == cursorCreatedAtUtc && a.Id < cursorId));
            }

            var page = await baseQuery
                .OrderByDescending(a => a.CreatedAtUtc)
                .ThenByDescending(a => a.Id)
                .Take(pageSize + 1)
                .ToListAsync();

            var hasNextPage = page.Count > pageSize;
            if (hasNextPage)
                page = page.Take(pageSize).ToList();

            var topLevelIds = page.Select(x => x.Id).ToList();

            var topLevelAttachments = await _context.LoanActivityAttachments
                .AsNoTracking()
                .Where(a => topLevelIds.Contains(a.LoanActivityId))
                .OrderByDescending(a => a.UploadedAtUtc)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            List<LoanActivity> replies = new();
            List<LoanActivityAttachment> replyAttachments = new();

            if (includeReplies && topLevelIds.Count > 0)
            {
                replies = await _context.LoanActivities
                    .AsNoTracking()
                    .Where(a => a.LoanId == loan.Id && a.ParentActivityId != null && topLevelIds.Contains(a.ParentActivityId.Value))
                    .OrderBy(a => a.CreatedAtUtc)
                    .ThenBy(a => a.Id)
                    .ToListAsync();

                var replyIds = replies.Select(r => r.Id).ToList();
                if (replyIds.Count > 0)
                {
                    replyAttachments = await _context.LoanActivityAttachments
                        .AsNoTracking()
                        .Where(a => replyIds.Contains(a.LoanActivityId))
                        .OrderByDescending(a => a.UploadedAtUtc)
                        .ThenByDescending(a => a.Id)
                        .ToListAsync();
                }
            }

            var replyByParent = replies
                .GroupBy(r => r.ParentActivityId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var responseItems = page.Select(activity =>
            {
                var mappedReplies = includeReplies && replyByParent.TryGetValue(activity.Id, out var groupedReplies)
                    ? groupedReplies.Select(reply => MapActivity(
                        reply,
                        replyAttachments.Where(a => a.LoanActivityId == reply.Id).ToList(),
                        Array.Empty<LoanActivityItemDto>()))
                        .ToList()
                    : new List<LoanActivityItemDto>();

                return MapActivity(
                    activity,
                    topLevelAttachments.Where(a => a.LoanActivityId == activity.Id).ToList(),
                    mappedReplies);
            }).ToList();

            string? nextCursor = null;
            if (hasNextPage && responseItems.Any())
            {
                var last = page.Last();
                nextCursor = EncodeCursor(last.CreatedAtUtc, last.Id);
            }

            var response = new LoanActivitiesResponseDto
            {
                Items = responseItems,
                NextCursor = nextCursor
            };

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> CreateActivity(string loanNumber, [FromBody] CreateLoanActivityRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var loan = await BuildLoanAccessQuery(userId, companyNmlsNumber, role)
                .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber);

            if (loan == null)
                return NotFound("Loan not found.");

            var activity = new LoanActivity
            {
                LoanId = loan.Id,
                LoanNumber = loan.LoanNumber,
                CompanyNmlsNumber = loan.CompanyNmlsNumber,
                ParentActivityId = null,
                ActivityType = request.Type,
                Message = request.Message,
                MetadataJson = request.Metadata?.GetRawText(),
                NotifyLoanTeam = request.NotifyLoanTeam,
                Visibility = LoanActivityVisibility.InternalOnly,
                ActorUserId = userId,
                ActorName = User.FindFirst(ClaimTypes.Name)?.Value,
                ActorRole = User.FindFirst(ClaimTypes.Role)?.Value,
                ActorType = LoanActivityActorType.User,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.LoanActivities.Add(activity);
            await _context.SaveChangesAsync();

            var response = MapActivity(
                activity,
                new List<LoanActivityAttachment>(),
                Array.Empty<LoanActivityItemDto>());

            return Ok(response);
        }

        [HttpPost("{activityId:int}/replies")]
        public async Task<IActionResult> CreateReply(
            string loanNumber,
            int activityId,
            [FromBody] CreateLoanActivityReplyRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var loan = await BuildLoanAccessQuery(userId, companyNmlsNumber, role)
                .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber);

            if (loan == null)
                return NotFound("Loan not found.");

            var parent = await _context.LoanActivities
                .FirstOrDefaultAsync(a =>
                    a.Id == activityId &&
                    a.LoanId == loan.Id &&
                    a.ParentActivityId == null);

            if (parent == null)
                return NotFound("Parent activity not found.");

            var reply = new LoanActivity
            {
                LoanId = loan.Id,
                LoanNumber = loan.LoanNumber,
                CompanyNmlsNumber = loan.CompanyNmlsNumber,
                ParentActivityId = parent.Id,
                ActivityType = LoanActivityType.Comment,
                Message = request.Message,
                NotifyLoanTeam = request.NotifyLoanTeam,
                Visibility = LoanActivityVisibility.InternalOnly,
                ActorUserId = userId,
                ActorName = User.FindFirst(ClaimTypes.Name)?.Value,
                ActorRole = User.FindFirst(ClaimTypes.Role)?.Value,
                ActorType = LoanActivityActorType.User,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.LoanActivities.Add(reply);
            await _context.SaveChangesAsync();

            var response = MapActivity(
                reply,
                new List<LoanActivityAttachment>(),
                Array.Empty<LoanActivityItemDto>());

            return Ok(response);
        }

        [HttpPost("{activityId:int}/attachments")]
        public async Task<IActionResult> UploadAttachment(
            string loanNumber,
            int activityId,
            [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (file.Length > MaxUploadSize)
                return BadRequest("Max upload size is 15MB.");

            if (!TryGetCurrentUser(out var userId, out var companyNmlsNumber, out var role))
                return Unauthorized("Missing required token claims.");

            var loan = await BuildLoanAccessQuery(userId, companyNmlsNumber, role)
                .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber);

            if (loan == null)
                return NotFound("Loan not found.");

            var activity = await _context.LoanActivities
                .FirstOrDefaultAsync(a => a.Id == activityId && a.LoanId == loan.Id);

            if (activity == null)
                return NotFound("Activity not found.");

            var originalFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            var contentType = file.ContentType.ToLowerInvariant();

            var allowedContentTypes = new[]
            {
                "application/pdf",
                "image/jpeg",
                "image/jpg",
                "image/png",
                "image/webp",
                "image/tiff",
                "text/plain",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            };

            var allowedExtensions = new[]
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png",
                ".webp",
                ".tif",
                ".tiff",
                ".txt",
                ".doc",
                ".docx"
            };

            if (!allowedContentTypes.Contains(contentType) || !allowedExtensions.Contains(extension))
                return BadRequest("Invalid file type.");

            if (contentType == "image/gif" || extension == ".gif")
                return BadRequest("GIF files are not allowed.");

            var tenantFolder = SanitizeFolderName(loan.CompanyNmlsNumber);
            var activityFolder = Path.Combine(
                "storage",
                "companies",
                tenantFolder,
                "loans",
                loan.LoanNumber,
                "activities",
                activity.Id.ToString());

            Directory.CreateDirectory(activityFolder);

            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(activityFolder, storedFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = Path.Combine(
                "companies",
                tenantFolder,
                "loans",
                loan.LoanNumber,
                "activities",
                activity.Id.ToString(),
                storedFileName);

            var attachment = new LoanActivityAttachment
            {
                CompanyNmlsNumber = loan.CompanyNmlsNumber,
                LoanId = loan.Id,
                LoanActivityId = activity.Id,
                AttachmentType = ResolveAttachmentType(contentType, extension),
                OriginalFileName = originalFileName,
                StoredFilePath = relativePath,
                ContentType = contentType,
                FileSize = new FileInfo(fullPath).Length,
                UploadedByUserId = userId,
                UploadedAtUtc = DateTime.UtcNow
            };

            _context.LoanActivityAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            var response = MapAttachment(attachment);
            return Ok(response);
        }

        private IQueryable<Loan> BuildLoanAccessQuery(int userId, string? companyNmlsNumber, string role)
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

        private bool TryGetCurrentUser(out int userId, out string? companyNmlsNumber, out string role)
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

        private static LoanActivityAttachmentType ResolveAttachmentType(string contentType, string extension)
        {
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return LoanActivityAttachmentType.Image;

            if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
                return LoanActivityAttachmentType.Pdf;

            return LoanActivityAttachmentType.Document;
        }

        private static string SanitizeFolderName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var filtered = value.Where(c => !invalid.Contains(c)).ToArray();
            return new string(filtered);
        }

        private static HashSet<LoanActivityType> ParseActivityTypes(string? types)
        {
            var result = new HashSet<LoanActivityType>();

            if (string.IsNullOrWhiteSpace(types))
                return result;

            var parts = types.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (Enum.TryParse<LoanActivityType>(part, true, out var parsed))
                    result.Add(parsed);
            }

            return result;
        }

        private static bool TryDecodeCursor(string? cursor, out DateTime createdAtUtc, out int id)
        {
            createdAtUtc = default;
            id = 0;

            if (string.IsNullOrWhiteSpace(cursor))
                return false;

            try
            {
                var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
                var parts = raw.Split('|');

                if (parts.Length != 2)
                    return false;

                if (!long.TryParse(parts[0], out var ticks))
                    return false;

                if (!int.TryParse(parts[1], out var parsedId))
                    return false;

                createdAtUtc = new DateTime(ticks, DateTimeKind.Utc);
                id = parsedId;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string EncodeCursor(DateTime createdAtUtc, int id)
        {
            var payload = $"{createdAtUtc.Ticks}|{id}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
        }

        private static LoanActivityItemDto MapActivity(
            LoanActivity activity,
            IReadOnlyCollection<LoanActivityAttachment> attachments,
            IEnumerable<LoanActivityItemDto> replies)
        {
            var replyList = replies.ToList();

            return new LoanActivityItemDto
            {
                Id = activity.Id,
                LoanNumber = activity.LoanNumber,
                ParentActivityId = activity.ParentActivityId,
                Type = activity.ActivityType.ToString(),
                Message = activity.Message,
                NotifyLoanTeam = activity.NotifyLoanTeam,
                Visibility = activity.Visibility.ToString(),
                ActorUserId = activity.ActorUserId,
                ActorName = activity.ActorName,
                ActorRole = activity.ActorRole,
                ActorType = activity.ActorType.ToString(),
                MetadataJson = activity.MetadataJson,
                CreatedAtUtc = activity.CreatedAtUtc,
                ReplyCount = replyList.Count,
                Attachments = attachments.Select(MapAttachment).ToList(),
                Replies = replyList
            };
        }

        private static LoanActivityAttachmentDto MapAttachment(LoanActivityAttachment attachment)
        {
            return new LoanActivityAttachmentDto
            {
                Id = attachment.Id,
                AttachmentType = attachment.AttachmentType.ToString(),
                OriginalFileName = attachment.OriginalFileName,
                StoredFilePath = attachment.StoredFilePath,
                ThumbnailFilePath = attachment.ThumbnailFilePath,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                UploadedAtUtc = attachment.UploadedAtUtc
            };
        }
    }
}
