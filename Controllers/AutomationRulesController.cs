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
    [Route("api/settings/automation-rules")]
    public class AutomationRulesController : AdminSettingsControllerBase
    {
        public AutomationRulesController(AppDbContext context) : base(context)
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

            var items = await Context.AutomationRules
                .AsNoTracking()
                .Where(x => x.CompanyNmlsNumber == company)
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.Name)
                .Select(x => Map(x))
                .ToListAsync(cancellationToken);

            return Ok(new AutomationRuleListResponse { Items = items });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] string? companyNmlsNumber, [FromBody] CreateAutomationRuleRequest request, CancellationToken cancellationToken)
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

            var rule = new AutomationRule
            {
                CompanyNmlsNumber = company,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                TriggerType = request.TriggerType?.Trim(),
                ActionType = request.ActionType?.Trim(),
                TriggerJson = request.TriggerJson,
                ActionJson = request.ActionJson,
                Milestone = request.Milestone?.Trim(),
                IsEnabled = request.IsEnabled,
                Priority = request.Priority,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedByUserId = userId
            };

            Context.AutomationRules.Add(rule);
            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "AutomationRuleCreated", "AutomationRules", new { rule.Id, rule.Name }, cancellationToken);

            return Ok(Map(rule));
        }

        [HttpPut("{ruleId:int}")]
        public async Task<IActionResult> Update(int ruleId, [FromQuery] string? companyNmlsNumber, [FromBody] UpdateAutomationRuleRequest request, CancellationToken cancellationToken)
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

            var rule = await Context.AutomationRules.FirstOrDefaultAsync(x => x.Id == ruleId && x.CompanyNmlsNumber == company, cancellationToken);
            if (rule == null)
                return NotFound("Automation rule not found.");

            rule.Name = request.Name.Trim();
            rule.Description = request.Description?.Trim();
            rule.TriggerType = request.TriggerType?.Trim();
            rule.ActionType = request.ActionType?.Trim();
            rule.TriggerJson = request.TriggerJson;
            rule.ActionJson = request.ActionJson;
            rule.Milestone = request.Milestone?.Trim();
            rule.IsEnabled = request.IsEnabled;
            rule.Priority = request.Priority;
            rule.UpdatedAtUtc = DateTime.UtcNow;
            rule.UpdatedByUserId = userId;

            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "AutomationRuleUpdated", "AutomationRules", new { rule.Id, rule.Name, rule.IsEnabled }, cancellationToken);

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

            var rule = await Context.AutomationRules.FirstOrDefaultAsync(x => x.Id == ruleId && x.CompanyNmlsNumber == company, cancellationToken);
            if (rule == null)
                return NotFound("Automation rule not found.");

            rule.IsEnabled = false;
            rule.UpdatedAtUtc = DateTime.UtcNow;
            rule.UpdatedByUserId = userId;

            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "AutomationRuleDeleted", "AutomationRules", new { rule.Id, rule.Name }, cancellationToken);

            return Ok(new { message = "Automation rule disabled." });
        }

        private static AutomationRuleResponse Map(AutomationRule rule)
        {
            return new AutomationRuleResponse
            {
                Id = rule.Id,
                CompanyNmlsNumber = rule.CompanyNmlsNumber,
                Name = rule.Name,
                Description = rule.Description,
                TriggerType = rule.TriggerType,
                ActionType = rule.ActionType,
                TriggerJson = rule.TriggerJson,
                ActionJson = rule.ActionJson,
                Milestone = rule.Milestone,
                IsEnabled = rule.IsEnabled,
                Priority = rule.Priority,
                CreatedAtUtc = rule.CreatedAtUtc,
                UpdatedAtUtc = rule.UpdatedAtUtc
            };
        }
    }
}