using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;

namespace PrestexaAPI.Services.ActivityLogs
{
    public sealed class ActivityLogService : IActivityLogService
    {
        private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "password",
            "token",
            "secret",
            "jwt",
            "authorization",
            "accessToken",
            "refreshToken",
            "clientSecret",
            "apiKey",
            "ssn",
            "socialSecurityNumber"
        };

        private static readonly Dictionary<string, ScopeDefinition> ScopeDefinitions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AdminSettings.Users"] = new ScopeDefinition(
                "Users",
                [
                    "UserCreated",
                    "UserUpdated",
                    "UserActivated",
                    "UserDeactivated",
                    "UserPrimaryRoleChanged",
                    "UserRoleAssigned",
                    "UserRoleDeactivated",
                    "UserRolesReplaced",
                    "UserProfilePhotoUploaded",
                    "ProfilePhotoUploaded",
                    "UserProfilePhotoRemoved",
                    "ProfilePhotoRemoved",
                    "PasswordResetSent",
                    "InviteSent",
                    "MFAStatusUpdated",
                    "BranchAssignmentUpdated"
                ]),
            ["AdminSettings.Roles"] = new ScopeDefinition(
                "Roles",
                ["RoleCreated", "RoleUpdated", "RoleDeactivated", "RolePermissionChanged"]),
            ["AdminSettings.Branches"] = new ScopeDefinition(
                "Branches",
                ["BranchCreated", "BranchUpdated", "BranchDeactivated", "BranchAssignmentUpdated"]),
            ["AdminSettings.Branding"] = new ScopeDefinition(
                "Branding",
                [
                    "BrandingUpdated",
                    "ApplicationLogoUpdated",
                    "POSLogoUpdated",
                    "EmailLogoUpdated",
                    "FaviconUpdated",
                    "BrandColorUpdated",
                    "PublicMediaAssetUploaded"
                ]),
            ["AdminSettings.Domains"] = new ScopeDefinition(
                "Domain",
                ["DomainAdded", "DomainVerified", "DomainRemoved", "TenantDomainUpdated", "DefaultDomainChanged"]),
            ["AdminSettings.Company"] = new ScopeDefinition(
                "Organization",
                ["OrganizationUpdated", "CompanyProfileUpdated", "CompanySettingsUpdated", "NmlsUpdated"]),
            ["AdminSettings.OperationalAssets"] = new ScopeDefinition(
                "Operational Assets",
                [
                    "OperationalAssetDownloaded",
                    "LoanEvidencePackageGenerated",
                    "LoanEvidencePackageDownloaded",
                    "CompanyExportPackageGenerated",
                    "CompanyExportPackageDownloaded",
                    "LegacyPathRepairDryRunExecuted"
                ]),
            ["AdminSettings.MessageProviderConfig"] = new ScopeDefinition(
                "Message Provider Config",
                ["MessageProviderConfigUpdated", "EmailTestMessageQueued", "SmsTestMessageQueued"]),
            ["MessageProviderConfig"] = new ScopeDefinition(
                "Message Provider Config",
                ["MessageProviderConfigUpdated", "EmailTestMessageQueued", "SmsTestMessageQueued"]),
            ["AdminSettings.EmailTracking"] = new ScopeDefinition(
                "Email Tracking",
                ["EmailNotificationEvent", "EmailTestMessageQueued"]),
            ["EmailTracking"] = new ScopeDefinition(
                "Email Tracking",
                ["EmailNotificationEvent", "EmailTestMessageQueued"]),
            ["AdminSettings.DocumentFolders"] = new ScopeDefinition(
                "Document Folders",
                ["DocumentFolderCreated", "DocumentFolderUpdated", "DocumentFolderDeleted"]),
            ["DocumentFolders"] = new ScopeDefinition(
                "Document Folders",
                ["DocumentFolderCreated", "DocumentFolderUpdated", "DocumentFolderDeleted"]),
            ["AdminSettings.Forms"] = new ScopeDefinition(
                "Forms",
                ["FormCreated", "FormUpdated", "FormDeleted"]),
            ["Forms"] = new ScopeDefinition(
                "Forms",
                ["FormCreated", "FormUpdated", "FormDeleted"]),
            ["AdminSettings.FormSets"] = new ScopeDefinition(
                "Form Sets",
                ["FormSetCreated", "FormSetUpdated", "FormSetDeleted"]),
            ["FormSets"] = new ScopeDefinition(
                "Form Sets",
                ["FormSetCreated", "FormSetUpdated", "FormSetDeleted"]),
            ["AdminSettings.ClientNeedsRules"] = new ScopeDefinition(
                "Client Needs Rules",
                ["ClientNeedsRuleCreated", "ClientNeedsRuleUpdated", "ClientNeedsRuleDeleted"]),
            ["ClientNeedsRules"] = new ScopeDefinition(
                "Client Needs Rules",
                ["ClientNeedsRuleCreated", "ClientNeedsRuleUpdated", "ClientNeedsRuleDeleted"]),
            ["AdminSettings.AutomationRules"] = new ScopeDefinition(
                "Automation Rules",
                ["AutomationRuleCreated", "AutomationRuleUpdated", "AutomationRuleDeleted"]),
            ["AutomationRules"] = new ScopeDefinition(
                "Automation Rules",
                ["AutomationRuleCreated", "AutomationRuleUpdated", "AutomationRuleDeleted"]),
            ["AdminSettings.ClosingCosts"] = new ScopeDefinition(
                "Closing Costs",
                ["ClosingCostCreated", "ClosingCostUpdated", "ClosingCostDeleted"]),
            ["ClosingCosts"] = new ScopeDefinition(
                "Closing Costs",
                ["ClosingCostCreated", "ClosingCostUpdated", "ClosingCostDeleted"]),
            ["AdminSettings.SmsTracking"] = new ScopeDefinition(
                "SMS Tracking",
                ["SmsTrackingViewed"]),
            ["SmsTracking"] = new ScopeDefinition(
                "SMS Tracking",
                ["SmsTrackingViewed"])
        };

        private readonly AppDbContext _context;

        public ActivityLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ActivityLogsResponse> GetActivityLogsAsync(ActivityLogsQuery query, CancellationToken cancellationToken)
        {
            var page = Math.Max(1, query.Page);
            var pageSize = query.Limit.HasValue
                ? Math.Clamp(query.Limit.Value, 1, 500)
                : Math.Clamp(query.PageSize, 1, 500);

            var items = new List<ActivityLogItemResponse>();

            await AppendOrganizationAuditAsync(items, query, cancellationToken);
            await AppendOperationalAssetAuditAsync(items, query, cancellationToken);
            await AppendLoanMilestoneHistoryAsync(items, query, cancellationToken);
            await AppendEmailDeliveryRecordsAsync(items, query, cancellationToken);

            await ResolveActorsAsync(items, cancellationToken);

            var filtered = ApplyFilters(items, query)
                .OrderByDescending(x => x.UpdatedAtUtc)
                .ThenByDescending(x => x.Id, StringComparer.Ordinal)
                .ToList();

            var totalCount = filtered.Count;
            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

            var paged = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new ActivityLogsResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Items = paged
            };
        }

        private async Task AppendOrganizationAuditAsync(List<ActivityLogItemResponse> items, ActivityLogsQuery query, CancellationToken cancellationToken)
        {
            var orgAuditQuery = _context.OrganizationAuditRecords
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.CompanyNmlsNumber))
            {
                var companyNmls = query.CompanyNmlsNumber.Trim();
                orgAuditQuery = orgAuditQuery.Where(x => x.CompanyNmlsNumber == companyNmls);
            }

            if (query.FromDate.HasValue)
                orgAuditQuery = orgAuditQuery.Where(x => x.ChangedAtUtc >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                orgAuditQuery = orgAuditQuery.Where(x => x.ChangedAtUtc <= query.ToDate.Value);

            var records = await orgAuditQuery
                .OrderByDescending(x => x.ChangedAtUtc)
                .Take(4000)
                .ToListAsync(cancellationToken);

            foreach (var record in records)
            {
                var eventName = NormalizeOrganizationEventName(record.Action, record.FieldName);
                var (section, activity) = GetSectionAndActivity(eventName, record.FieldName);

                var details = new Dictionary<string, object?>();
                if (!string.IsNullOrWhiteSpace(record.FieldName))
                    details["fieldName"] = record.FieldName;

                if (record.TargetUserId.HasValue)
                    details["targetUserId"] = record.TargetUserId.Value;

                if (record.OldAssetId.HasValue)
                    details["oldAssetId"] = record.OldAssetId.Value;

                if (record.NewAssetId.HasValue)
                    details["newAssetId"] = record.NewAssetId.Value;

                MergeDetailValue(details, "oldValue", record.OldValue);
                MergeDetailValue(details, "newValue", record.NewValue);

                var entityType = ResolveOrganizationEntityType(eventName, record);
                var entityId = ResolveOrganizationEntityId(eventName, record);

                items.Add(new ActivityLogItemResponse
                {
                    Id = $"organization-audit-{record.Id}",
                    Section = section,
                    Activity = activity,
                    Summary = BuildOrganizationSummary(eventName, record),
                    UpdatedAtUtc = record.ChangedAtUtc,
                    EntityType = entityType,
                    EntityId = entityId,
                    EventName = eventName,
                    Details = RedactSensitiveData(details),
                    ActorUserId = record.ChangedByUserId,
                    CompanyNmlsNumber = record.CompanyNmlsNumber,
                    LoanNumber = TryExtractLoanNumber(details)
                });
            }
        }

        private async Task AppendOperationalAssetAuditAsync(List<ActivityLogItemResponse> items, ActivityLogsQuery query, CancellationToken cancellationToken)
        {
            var operationalAuditQuery = _context.OperationalAssetAuditRecords
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.CompanyNmlsNumber))
            {
                var companyNmls = query.CompanyNmlsNumber.Trim();
                operationalAuditQuery = operationalAuditQuery.Where(x => x.CompanyNmlsNumber == companyNmls);
            }

            if (!string.IsNullOrWhiteSpace(query.LoanNumber))
            {
                var loanNumber = query.LoanNumber.Trim();
                operationalAuditQuery = operationalAuditQuery.Where(x => x.LoanNumber == loanNumber);
            }

            if (query.FromDate.HasValue)
                operationalAuditQuery = operationalAuditQuery.Where(x => x.DownloadedAtUtc >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                operationalAuditQuery = operationalAuditQuery.Where(x => x.DownloadedAtUtc <= query.ToDate.Value);

            var records = await operationalAuditQuery
                .OrderByDescending(x => x.DownloadedAtUtc)
                .Take(4000)
                .ToListAsync(cancellationToken);

            foreach (var record in records)
            {
                var details = new Dictionary<string, object?>
                {
                    ["assetId"] = record.OperationalAssetId,
                    ["fileName"] = record.FileName,
                    ["assetType"] = record.AssetType.ToString(),
                    ["assetCategory"] = record.AssetCategory.ToString(),
                    ["fileSizeBytes"] = record.FileSizeBytes,
                    ["contentType"] = record.ContentType,
                    ["publicId"] = record.PublicId,
                    ["loanId"] = record.LoanId,
                    ["loanNumber"] = record.LoanNumber,
                    ["borrowerId"] = record.BorrowerId
                };

                MergeDetailValue(details, "metadata", record.MetadataJson);

                items.Add(new ActivityLogItemResponse
                {
                    Id = $"operational-asset-audit-{record.Id}",
                    Section = "Operational Assets",
                    Activity = EventNameToActivity(record.EventName),
                    Summary = BuildOperationalAssetSummary(record),
                    UpdatedAtUtc = record.DownloadedAtUtc,
                    EntityType = "OperationalAsset",
                    EntityId = record.OperationalAssetId?.ToString() ?? record.PublicId.ToString(),
                    EventName = record.EventName,
                    Details = RedactSensitiveData(details),
                    ActorUserId = record.DownloadedByUserId,
                    CompanyNmlsNumber = record.CompanyNmlsNumber,
                    LoanNumber = record.LoanNumber
                });
            }
        }

        private async Task AppendLoanMilestoneHistoryAsync(List<ActivityLogItemResponse> items, ActivityLogsQuery query, CancellationToken cancellationToken)
        {
            var milestoneQuery = _context.LoanMilestoneHistories
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.CompanyNmlsNumber))
            {
                var companyNmls = query.CompanyNmlsNumber.Trim();
                milestoneQuery = milestoneQuery.Where(x => x.CompanyNmlsNumber == companyNmls);
            }

            if (!string.IsNullOrWhiteSpace(query.LoanNumber))
            {
                var loanNumber = query.LoanNumber.Trim();
                milestoneQuery = milestoneQuery.Where(x => x.LoanNumber == loanNumber);
            }

            if (query.FromDate.HasValue)
                milestoneQuery = milestoneQuery.Where(x => x.ChangedAtUtc >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                milestoneQuery = milestoneQuery.Where(x => x.ChangedAtUtc <= query.ToDate.Value);

            var records = await milestoneQuery
                .OrderByDescending(x => x.ChangedAtUtc)
                .Take(2000)
                .ToListAsync(cancellationToken);

            foreach (var record in records)
            {
                var details = new Dictionary<string, object?>
                {
                    ["oldMilestone"] = record.OldMilestone,
                    ["newMilestone"] = record.NewMilestone,
                    ["changeSource"] = record.ChangeSource.ToString(),
                    ["notes"] = record.Notes,
                    ["loanId"] = record.LoanId,
                    ["loanNumber"] = record.LoanNumber
                };

                MergeDetailValue(details, "metadata", record.MetadataJson);

                items.Add(new ActivityLogItemResponse
                {
                    Id = $"loan-milestone-history-{record.Id}",
                    Section = "Loans",
                    Activity = "Loan Milestone Updated",
                    Summary = $"Milestone changed from {record.OldMilestone ?? "Unknown"} to {record.NewMilestone}",
                    UpdatedAtUtc = record.ChangedAtUtc,
                    EntityType = "Loan",
                    EntityId = record.LoanId.ToString(),
                    EventName = "LoanMilestoneChanged",
                    Details = RedactSensitiveData(details),
                    ActorUserId = record.ChangedByUserId,
                    CompanyNmlsNumber = record.CompanyNmlsNumber,
                    LoanNumber = record.LoanNumber
                });
            }
        }

        private async Task AppendEmailDeliveryRecordsAsync(List<ActivityLogItemResponse> items, ActivityLogsQuery query, CancellationToken cancellationToken)
        {
            var emailQuery = _context.EmailDeliveryRecords
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.CompanyNmlsNumber))
            {
                var companyNmls = query.CompanyNmlsNumber.Trim();
                emailQuery = emailQuery.Where(x => x.CompanyNmlsNumber == companyNmls);
            }

            if (!string.IsNullOrWhiteSpace(query.LoanNumber))
            {
                var loanNumber = query.LoanNumber.Trim();
                emailQuery = emailQuery.Where(x => x.LoanNumber == loanNumber);
            }

            if (query.FromDate.HasValue)
                emailQuery = emailQuery.Where(x =>
                    (x.DeliveredAtUtc ?? x.SentAtUtc ?? x.FailedAtUtc ?? DateTime.MinValue) >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                emailQuery = emailQuery.Where(x =>
                    (x.DeliveredAtUtc ?? x.SentAtUtc ?? x.FailedAtUtc ?? DateTime.MinValue) <= query.ToDate.Value);

            var records = await emailQuery
                .OrderByDescending(x => x.DeliveredAtUtc ?? x.SentAtUtc ?? x.FailedAtUtc)
                .Take(2000)
                .ToListAsync(cancellationToken);

            foreach (var record in records)
            {
                var updatedAtUtc = record.DeliveredAtUtc ?? record.SentAtUtc ?? record.FailedAtUtc ?? DateTime.UtcNow;
                var details = new Dictionary<string, object?>
                {
                    ["recipientEmail"] = record.RecipientEmail,
                    ["recipientName"] = record.RecipientName,
                    ["recipientType"] = record.RecipientType.ToString(),
                    ["templateKey"] = record.TemplateKey,
                    ["subject"] = record.Subject,
                    ["status"] = record.Status.ToString(),
                    ["providerMessageId"] = record.ProviderMessageId,
                    ["triggeredByEvent"] = record.TriggeredByEvent,
                    ["loanId"] = record.LoanId,
                    ["loanNumber"] = record.LoanNumber
                };

                MergeDetailValue(details, "metadata", record.MetadataJson);

                items.Add(new ActivityLogItemResponse
                {
                    Id = $"email-delivery-{record.Id}",
                    Section = "Email Tracking",
                    Activity = "Automated Email Event",
                    Summary = $"{record.Status} email to {record.RecipientEmail} (template {record.TemplateKey})",
                    UpdatedAtUtc = updatedAtUtc,
                    EntityType = record.LoanId.HasValue ? "Loan" : "Company",
                    EntityId = record.LoanId?.ToString(),
                    EventName = string.IsNullOrWhiteSpace(record.TriggeredByEvent)
                        ? "EmailNotificationEvent"
                        : record.TriggeredByEvent,
                    Details = RedactSensitiveData(details),
                    ActorUserId = record.TriggeredByUserId,
                    CompanyNmlsNumber = record.CompanyNmlsNumber,
                    LoanNumber = record.LoanNumber
                });
            }
        }

        private async Task ResolveActorsAsync(List<ActivityLogItemResponse> items, CancellationToken cancellationToken)
        {
            var actorIds = items
                .Where(x => x.ActorUserId.HasValue)
                .Select(x => x.ActorUserId!.Value)
                .Distinct()
                .ToList();

            if (actorIds.Count == 0)
            {
                foreach (var item in items)
                {
                    item.ActorName = "System";
                    item.ActorEmail = null;
                    item.ActorAvatarUrl = null;
                }

                return;
            }

            var users = await _context.Users
                .AsNoTracking()
                .Where(x => actorIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    x.Email,
                    x.ProfilePhotoAssetId
                })
                .ToListAsync(cancellationToken);

            var mediaAssetIds = users
                .Where(x => x.ProfilePhotoAssetId.HasValue)
                .Select(x => x.ProfilePhotoAssetId!.Value)
                .Distinct()
                .ToList();

            var mediaAssets = mediaAssetIds.Count == 0
                ? new Dictionary<int, Guid>()
                : await _context.MediaAssets
                    .AsNoTracking()
                    .Where(x => mediaAssetIds.Contains(x.Id) && x.IsActive)
                    .ToDictionaryAsync(x => x.Id, x => x.PublicId, cancellationToken);

            var actorMap = users.ToDictionary(
                x => x.Id,
                x => new
                {
                    Name = BuildDisplayName(x.FirstName, x.LastName),
                    x.Email,
                    AvatarUrl = x.ProfilePhotoAssetId.HasValue && mediaAssets.TryGetValue(x.ProfilePhotoAssetId.Value, out var publicId)
                        ? $"https://api.prestexa.com/api/media/asset/{publicId}"
                        : (string?)null
                });

            foreach (var item in items)
            {
                if (!item.ActorUserId.HasValue)
                {
                    item.ActorName = "System";
                    item.ActorEmail = null;
                    item.ActorAvatarUrl = null;
                    continue;
                }

                if (!actorMap.TryGetValue(item.ActorUserId.Value, out var actor))
                {
                    item.ActorName = "System";
                    item.ActorEmail = null;
                    item.ActorAvatarUrl = null;
                    continue;
                }

                item.ActorName = string.IsNullOrWhiteSpace(actor.Name) ? "System" : actor.Name;
                item.ActorEmail = actor.Email;
                item.ActorAvatarUrl = actor.AvatarUrl;
            }
        }

        private static IEnumerable<ActivityLogItemResponse> ApplyFilters(IEnumerable<ActivityLogItemResponse> items, ActivityLogsQuery query)
        {
            var filtered = items;

            if (!string.IsNullOrWhiteSpace(query.Scope))
                filtered = filtered.Where(x => MatchesScope(query.Scope.Trim(), x));

            if (!string.IsNullOrWhiteSpace(query.EntityType))
                filtered = filtered.Where(x => string.Equals(x.EntityType, query.EntityType.Trim(), StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(query.EntityId))
                filtered = filtered.Where(x => string.Equals(x.EntityId, query.EntityId.Trim(), StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(query.LoanNumber))
                filtered = filtered.Where(x => string.Equals(x.LoanNumber, query.LoanNumber.Trim(), StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(query.EventName))
                filtered = filtered.Where(x => string.Equals(x.EventName, query.EventName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                filtered = filtered.Where(x =>
                    ContainsIgnoreCase(x.Section, search) ||
                    ContainsIgnoreCase(x.Activity, search) ||
                    ContainsIgnoreCase(x.Summary, search) ||
                    ContainsIgnoreCase(x.EventName, search) ||
                    ContainsIgnoreCase(x.ActorName, search) ||
                    ContainsIgnoreCase(x.ActorEmail, search));
            }

            if (query.FromDate.HasValue)
                filtered = filtered.Where(x => x.UpdatedAtUtc >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                filtered = filtered.Where(x => x.UpdatedAtUtc <= query.ToDate.Value);

            return filtered;
        }

        private static bool ContainsIgnoreCase(string? value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesScope(string scope, ActivityLogItemResponse item)
        {
            if (!ScopeDefinitions.TryGetValue(scope, out var definition))
                return false;

            return definition.Events.Contains(item.EventName) ||
                   string.Equals(definition.Section, item.Section, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeOrganizationEventName(string action, string? fieldName)
        {
            if (string.IsNullOrWhiteSpace(action))
                return "OrganizationAuditEvent";

            var normalized = NormalizeKey(action);
            var normalizedField = NormalizeKey(fieldName);

            return normalized switch
            {
                "usercreated" => "UserCreated",
                "userupdated" => "UserUpdated",
                "useractivated" => "UserActivated",
                "userdeactivated" => "UserDeactivated",
                "userprimaryrolechanged" => "UserPrimaryRoleChanged",
                "userroleassigned" => "UserRoleAssigned",
                "userroledeactivated" => "UserRoleDeactivated",
                "userrolesreplaced" => "UserRolesReplaced",
                "profilephotouploaded" => "ProfilePhotoUploaded",
                "adminupdateduserprofilephoto" => "ProfilePhotoUploaded",
                "userprofilephotouploaded" => "ProfilePhotoUploaded",
                "profilephotoremoved" => "ProfilePhotoRemoved",
                "adminremoveduserprofilephoto" => "ProfilePhotoRemoved",
                "userprofilephotoremoved" => "ProfilePhotoRemoved",
                "branchassignmentupdated" => "BranchAssignmentUpdated",
                "invitesent" => "InviteSent",
                "passwordresetsent" => "PasswordResetSent",
                "domaincreated" => "DomainAdded",
                "domainupdated" => "TenantDomainUpdated",
                "domainverified" => "DomainVerified",
                "domainremoved" => "DomainRemoved",
                "defaultdomainchanged" => "DefaultDomainChanged",
                "brandingupdated" => "BrandingUpdated",
                "applicationlogoupdated" => "ApplicationLogoUpdated",
                "poslogoupdated" => "POSLogoUpdated",
                "emaillogoupdated" => "EmailLogoUpdated",
                "faviconupdated" => "FaviconUpdated",
                "brandcolorupdated" => "BrandColorUpdated",
                "publicmediaassetuploaded" => "PublicMediaAssetUploaded",
                "companysettingsupdated" => "CompanySettingsUpdated",
                "organizationupdated" => "OrganizationUpdated",
                "companyprofileupdated" => "CompanyProfileUpdated",
                "nmlsupdated" => "NmlsUpdated",
                "rolecreated" => "RoleCreated",
                "roleupdated" => "RoleUpdated",
                "roledeactivated" => "RoleDeactivated",
                "rolepermissionchanged" => "RolePermissionChanged",
                "branchcreated" => "BranchCreated",
                "branchupdated" => "BranchUpdated",
                "branchdeactivated" => "BranchDeactivated",
                _ => InferOrganizationEventName(action, normalizedField)
            };
        }

        private static string InferOrganizationEventName(string action, string normalizedField)
        {
            if (normalizedField.Contains("domain", StringComparison.Ordinal))
                return "TenantDomainUpdated";

            if (normalizedField.Contains("branding", StringComparison.Ordinal) ||
                normalizedField.Contains("logo", StringComparison.Ordinal) ||
                normalizedField.Contains("color", StringComparison.Ordinal))
                return "BrandingUpdated";

            if (normalizedField.Contains("user", StringComparison.Ordinal))
                return "UserUpdated";

            if (normalizedField.Contains("role", StringComparison.Ordinal))
                return "RoleUpdated";

            if (normalizedField.Contains("branch", StringComparison.Ordinal))
                return "BranchUpdated";

            if (NormalizeKey(action).Contains("changed", StringComparison.Ordinal))
                return "CompanySettingsUpdated";

            return ToPascalCase(action);
        }

        private static string ResolveOrganizationEntityType(string eventName, OrganizationAuditRecord record)
        {
            if (record.TargetUserId.HasValue)
                return "User";

            if (record.NewAssetId.HasValue || record.OldAssetId.HasValue)
                return "MediaAsset";

            if (eventName.StartsWith("Role", StringComparison.OrdinalIgnoreCase))
                return "Role";

            if (eventName.StartsWith("Branch", StringComparison.OrdinalIgnoreCase))
                return "Branch";

            if (eventName.StartsWith("Domain", StringComparison.OrdinalIgnoreCase) ||
                eventName.Contains("Domain", StringComparison.OrdinalIgnoreCase))
                return "Domain";

            if (eventName.Contains("Brand", StringComparison.OrdinalIgnoreCase) ||
                eventName.Contains("Logo", StringComparison.OrdinalIgnoreCase) ||
                eventName.Contains("Favicon", StringComparison.OrdinalIgnoreCase))
                return "Branding";

            return "Organization";
        }

        private static string ResolveOrganizationEntityId(string eventName, OrganizationAuditRecord record)
        {
            if (record.TargetUserId.HasValue)
                return record.TargetUserId.Value.ToString();

            if (record.NewAssetId.HasValue)
                return record.NewAssetId.Value.ToString();

            if (record.OldAssetId.HasValue)
                return record.OldAssetId.Value.ToString();

            return record.OrganizationId.ToString();
        }

        private static string BuildOrganizationSummary(string eventName, OrganizationAuditRecord record)
        {
            return eventName switch
            {
                "UserPrimaryRoleChanged" => "User primary role was changed",
                "UserRolesReplaced" => "User roles were updated",
                "UserRoleAssigned" => "User role assigned",
                "UserRoleDeactivated" => "User role deactivated",
                "UserCreated" => "User account created",
                "UserUpdated" => "User account updated",
                "UserActivated" => "User activated",
                "UserDeactivated" => "User deactivated",
                "ProfilePhotoUploaded" => "User profile photo uploaded",
                "ProfilePhotoRemoved" => "User profile photo removed",
                "BranchAssignmentUpdated" => "User branch assignments were updated",
                "DomainAdded" => "Domain added",
                "DomainVerified" => "Domain verified",
                "TenantDomainUpdated" => "Tenant domain updated",
                "BrandingUpdated" => "Branding was updated",
                "ApplicationLogoUpdated" => "Application logo updated",
                "POSLogoUpdated" => "POS logo updated",
                "CompanySettingsUpdated" => "Company settings were updated",
                _ => string.IsNullOrWhiteSpace(record.FieldName)
                    ? EventNameToActivity(eventName)
                    : $"{record.FieldName} updated"
            };
        }

        private static string BuildOperationalAssetSummary(OperationalAssetAuditRecord record)
        {
            var fileName = string.IsNullOrWhiteSpace(record.FileName)
                ? "asset"
                : record.FileName;

            return record.EventName switch
            {
                "OperationalAssetDownloaded" => $"Operational asset downloaded: {fileName}",
                "LoanEvidencePackageGenerated" => $"Loan evidence package generated: {fileName}",
                "LoanEvidencePackageDownloaded" => $"Loan evidence package downloaded: {fileName}",
                "CompanyExportPackageGenerated" => $"Company export package generated: {fileName}",
                "CompanyExportPackageDownloaded" => $"Company export package downloaded: {fileName}",
                "LegacyPathRepairDryRunExecuted" => "Legacy path repair dry-run executed",
                _ => EventNameToActivity(record.EventName)
            };
        }

        private static (string Section, string Activity) GetSectionAndActivity(string eventName, string? fieldName)
        {
            if (ScopeDefinitions["AdminSettings.Users"].Events.Contains(eventName))
                return ("Users", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.Roles"].Events.Contains(eventName))
                return ("Roles", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.Branches"].Events.Contains(eventName))
                return ("Branches", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.Branding"].Events.Contains(eventName))
                return ("Branding", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.Domains"].Events.Contains(eventName))
                return ("Domain", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.MessageProviderConfig"].Events.Contains(eventName))
                return ("Message Provider Config", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.EmailTracking"].Events.Contains(eventName))
                return ("Email Tracking", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.DocumentFolders"].Events.Contains(eventName))
                return ("Document Folders", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.Forms"].Events.Contains(eventName))
                return ("Forms", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.FormSets"].Events.Contains(eventName))
                return ("Form Sets", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.ClientNeedsRules"].Events.Contains(eventName))
                return ("Client Needs Rules", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.AutomationRules"].Events.Contains(eventName))
                return ("Automation Rules", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.ClosingCosts"].Events.Contains(eventName))
                return ("Closing Costs", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.SmsTracking"].Events.Contains(eventName))
                return ("SMS Tracking", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.OperationalAssets"].Events.Contains(eventName))
                return ("Operational Assets", EventNameToActivity(eventName));

            if (ScopeDefinitions["AdminSettings.Company"].Events.Contains(eventName))
                return ("Organization", EventNameToActivity(eventName));

            if (!string.IsNullOrWhiteSpace(fieldName))
            {
                var normalizedField = NormalizeKey(fieldName);
                if (normalizedField.Contains("domain", StringComparison.Ordinal))
                    return ("Domain", EventNameToActivity(eventName));

                if (normalizedField.Contains("brand", StringComparison.Ordinal) ||
                    normalizedField.Contains("logo", StringComparison.Ordinal) ||
                    normalizedField.Contains("color", StringComparison.Ordinal))
                    return ("Branding", EventNameToActivity(eventName));

                if (normalizedField.Contains("role", StringComparison.Ordinal))
                    return ("Roles", EventNameToActivity(eventName));

                if (normalizedField.Contains("branch", StringComparison.Ordinal))
                    return ("Branches", EventNameToActivity(eventName));

                if (normalizedField.Contains("user", StringComparison.Ordinal))
                    return ("Users", EventNameToActivity(eventName));
            }

            return ("Organization", EventNameToActivity(eventName));
        }

        private static string EventNameToActivity(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                return "Activity Updated";

            var builder = new StringBuilder(eventName.Length + 8);
            for (var index = 0; index < eventName.Length; index++)
            {
                var ch = eventName[index];
                if (index > 0 && char.IsUpper(ch) && !char.IsWhiteSpace(eventName[index - 1]))
                    builder.Append(' ');

                builder.Append(ch);
            }

            return builder.ToString().Trim();
        }

        private static void MergeDetailValue(Dictionary<string, object?> details, string key, string? jsonOrValue)
        {
            if (string.IsNullOrWhiteSpace(jsonOrValue))
                return;

            var parsed = ParseJsonOrString(jsonOrValue);

            if (parsed is Dictionary<string, object?> dictionary)
            {
                foreach (var pair in dictionary)
                {
                    if (!details.ContainsKey(pair.Key))
                        details[pair.Key] = pair.Value;
                }

                return;
            }

            details[key] = parsed;
        }

        private static object? ParseJsonOrString(string value)
        {
            try
            {
                using var document = JsonDocument.Parse(value);
                return ConvertJsonElement(document.RootElement);
            }
            catch
            {
                return value;
            }
        }

        private static object? ConvertJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                    x => x.Name,
                    x => ConvertJsonElement(x.Value),
                    StringComparer.OrdinalIgnoreCase),
                JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var longValue) ? longValue : element.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.ToString()
            };
        }

        private static object RedactSensitiveData(object value)
        {
            return RedactSensitiveDataInternal(value, null) ?? new { };
        }

        private static object? RedactSensitiveDataInternal(object? value, string? key)
        {
            if (ShouldRedactKey(key))
                return "[redacted]";

            if (value is Dictionary<string, object?> dictionary)
            {
                var redacted = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in dictionary)
                {
                    redacted[pair.Key] = RedactSensitiveDataInternal(pair.Value, pair.Key);
                }

                return redacted;
            }

            if (value is IEnumerable<object?> enumerable && value is not string)
            {
                return enumerable
                    .Select(x => RedactSensitiveDataInternal(x, key))
                    .ToList();
            }

            return value;
        }

        private static bool ShouldRedactKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            return SensitiveKeys.Any(x => key.Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var chars = value.Where(char.IsLetterOrDigit).ToArray();
            return new string(chars).ToLowerInvariant();
        }

        private static string ToPascalCase(string value)
        {
            var segments = value
                .Split(new[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (segments.Length == 0)
                return "AuditEvent";

            var builder = new StringBuilder();
            foreach (var segment in segments)
            {
                builder.Append(char.ToUpperInvariant(segment[0]));
                if (segment.Length > 1)
                    builder.Append(segment[1..]);
            }

            return builder.ToString();
        }

        private static string? TryExtractLoanNumber(Dictionary<string, object?> details)
        {
            if (!details.TryGetValue("loanNumber", out var value) || value == null)
                return null;

            return value.ToString();
        }

        private static string BuildDisplayName(string? firstName, string? lastName)
        {
            return string.Join(" ", new[] { firstName, lastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private sealed record ScopeDefinition(string Section, HashSet<string> Events)
        {
            public ScopeDefinition(string section, IEnumerable<string> events)
                : this(section, new HashSet<string>(events, StringComparer.OrdinalIgnoreCase))
            {
            }
        }
    }
}
