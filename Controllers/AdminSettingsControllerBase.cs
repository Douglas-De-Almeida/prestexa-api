using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using System.Security.Claims;
using System.Text.Json;

namespace PrestexaAPI.Controllers
{
    public abstract class AdminSettingsControllerBase : ControllerBase
    {
        protected static readonly string[] AdminRoles = ["Owner", "Company Admin", "Associate Admin", "Branch Admin", "SuperAdmin"];

        protected readonly AppDbContext Context;

        protected AdminSettingsControllerBase(AppDbContext context)
        {
            Context = context;
        }

        protected bool TryGetCurrentUser(out int userId, out string? companyNmlsNumber, out string role)
        {
            userId = 0;
            companyNmlsNumber = User.FindFirst("CompanyNmlsNumber")?.Value;
            role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            var userIdValue = User.FindFirst("UserId")?.Value;
            return int.TryParse(userIdValue, out userId);
        }

        protected bool IsSuperAdmin()
        {
            return string.Equals(User.FindFirst(ClaimTypes.Role)?.Value, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        }

        protected static string? ResolveCompanyScope(string? requestedCompanyNmls, string? currentCompanyNmls)
        {
            var companyNmls = string.IsNullOrWhiteSpace(requestedCompanyNmls)
                ? currentCompanyNmls
                : requestedCompanyNmls.Trim();

            return string.IsNullOrWhiteSpace(companyNmls) ? null : companyNmls;
        }

        protected async Task AddAuditAsync(string companyNmlsNumber, int userId, string action, string fieldName, object payload, CancellationToken cancellationToken)
        {
            var organizationId = await Context.Companies
                .Where(x => x.NmlsNumber == companyNmlsNumber)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (organizationId <= 0)
                return;

            Context.OrganizationAuditRecords.Add(new OrganizationAuditRecord
            {
                OrganizationId = organizationId,
                CompanyNmlsNumber = companyNmlsNumber,
                Action = action,
                FieldName = fieldName,
                NewValue = JsonSerializer.Serialize(payload),
                ChangedByUserId = userId,
                ChangedAtUtc = DateTime.UtcNow
            });

            await Context.SaveChangesAsync(cancellationToken);
        }
    }
}