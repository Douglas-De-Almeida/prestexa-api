using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;
using PrestexaAPI.Models.Responses;

namespace PrestexaAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/settings/client-needs-rules")]
    public class ClientNeedsRulesController : AdminSettingsControllerBase
    {
        public ClientNeedsRulesController(AppDbContext context) : base(context)
        {
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? companyNmlsNumber, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out _, out var currentCompanyNmls, out var role))
                return Unauthorized();

            if (!AdminRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                return Forbid();

            if (!IsSuperAdmin() && !string.IsNullOrWhiteSpace(companyNmlsNumber) && !string.Equals(companyNmlsNumber.Trim(), currentCompanyNmls, StringComparison.Ordinal))
                return NotFound();

            var company = ResolveCompanyScope(companyNmlsNumber, currentCompanyNmls);
            if (company == null)
                return Unauthorized();

            var items = await Context.ClientNeedsRules
                .AsNoTracking()
                .Where(x => x.CompanyNmlsNumber == company)
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.Name)
                .Select(x => Map(x))
                .ToListAsync(cancellationToken);

            return Ok(new ClientNeedsRuleListResponse { Items = items });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] string? companyNmlsNumber, [FromBody] CreateClientNeedsRuleRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out var currentCompanyNmls, out var role))
                return Unauthorized();

            if (!AdminRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                return Forbid();

            if (!IsSuperAdmin() && !string.IsNullOrWhiteSpace(companyNmlsNumber) && !string.Equals(companyNmlsNumber.Trim(), currentCompanyNmls, StringComparison.Ordinal))
                return NotFound();

            var company = ResolveCompanyScope(companyNmlsNumber, currentCompanyNmls);
            if (company == null)
                return Unauthorized();

            var rule = new ClientNeedsRule
            {
                CompanyNmlsNumber = company,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                TriggerEvent = request.TriggerEvent?.Trim(),
                ConditionJson = request.ConditionJson,
                RequestedDocumentsJson = request.RequestedDocumentsJson,
                TargetRecipientType = request.TargetRecipientType?.Trim(),
                Milestone = request.Milestone?.Trim(),
                ReminderEnabled = request.ReminderEnabled,
                IsEnabled = request.IsEnabled,
                Priority = request.Priority,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedByUserId = userId
            };

            Context.ClientNeedsRules.Add(rule);
            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "ClientNeedsRuleCreated", "ClientNeedsRules", new { rule.Id, rule.Name }, cancellationToken);

            return Ok(Map(rule));
        }

        [HttpPut("{ruleId:int}")]
        public async Task<IActionResult> Update(int ruleId, [FromQuery] string? companyNmlsNumber, [FromBody] UpdateClientNeedsRuleRequest request, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out var currentCompanyNmls, out var role))
                return Unauthorized();

            if (!AdminRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                return Forbid();

            if (!IsSuperAdmin() && !string.IsNullOrWhiteSpace(companyNmlsNumber) && !string.Equals(companyNmlsNumber.Trim(), currentCompanyNmls, StringComparison.Ordinal))
                return NotFound();

            var company = ResolveCompanyScope(companyNmlsNumber, currentCompanyNmls);
            if (company == null)
                return Unauthorized();

            var rule = await Context.ClientNeedsRules.FirstOrDefaultAsync(x => x.Id == ruleId && x.CompanyNmlsNumber == company, cancellationToken);
            if (rule == null)
                return NotFound("Client needs rule not found.");

            rule.Name = request.Name.Trim();
            rule.Description = request.Description?.Trim();
            rule.TriggerEvent = request.TriggerEvent?.Trim();
            rule.ConditionJson = request.ConditionJson;
            rule.RequestedDocumentsJson = request.RequestedDocumentsJson;
            rule.TargetRecipientType = request.TargetRecipientType?.Trim();
            rule.Milestone = request.Milestone?.Trim();
            rule.ReminderEnabled = request.ReminderEnabled;
            rule.IsEnabled = request.IsEnabled;
            rule.Priority = request.Priority;
            rule.UpdatedAtUtc = DateTime.UtcNow;
            rule.UpdatedByUserId = userId;

            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "ClientNeedsRuleUpdated", "ClientNeedsRules", new { rule.Id, rule.Name, rule.IsEnabled }, cancellationToken);

            return Ok(Map(rule));
        }

        [HttpDelete("{ruleId:int}")]
        public async Task<IActionResult> Delete(int ruleId, [FromQuery] string? companyNmlsNumber, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out var currentCompanyNmls, out var role))
                return Unauthorized();

            if (!AdminRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                return Forbid();

            var company = ResolveCompanyScope(companyNmlsNumber, currentCompanyNmls);
            if (company == null)
                return Unauthorized();

            var rule = await Context.ClientNeedsRules.FirstOrDefaultAsync(x => x.Id == ruleId && x.CompanyNmlsNumber == company, cancellationToken);
            if (rule == null)
                return NotFound("Client needs rule not found.");

            rule.IsEnabled = false;
            rule.UpdatedAtUtc = DateTime.UtcNow;
            rule.UpdatedByUserId = userId;

            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "ClientNeedsRuleDeleted", "ClientNeedsRules", new { rule.Id, rule.Name }, cancellationToken);

            return Ok(new { message = "Client needs rule disabled." });
        }

        private static ClientNeedsRuleResponse Map(ClientNeedsRule rule)
        {
            return new ClientNeedsRuleResponse
            {
                Id = rule.Id,
                CompanyNmlsNumber = rule.CompanyNmlsNumber,
                Name = rule.Name,
                Description = rule.Description,
                TriggerEvent = rule.TriggerEvent,
                ConditionJson = rule.ConditionJson,
                RequestedDocumentsJson = rule.RequestedDocumentsJson,
                TargetRecipientType = rule.TargetRecipientType,
                Milestone = rule.Milestone,
                ReminderEnabled = rule.ReminderEnabled,
                IsEnabled = rule.IsEnabled,
                Priority = rule.Priority,
                CreatedAtUtc = rule.CreatedAtUtc,
                UpdatedAtUtc = rule.UpdatedAtUtc
            };
        }
    }
}