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
    [Route("api/settings/forms")]
    public class FormsController : AdminSettingsControllerBase
    {
        public FormsController(AppDbContext context) : base(context)
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

            var items = await Context.FormDefinitions
                .AsNoTracking()
                .Where(x => x.CompanyNmlsNumber == company)
                .OrderBy(x => x.Name)
                .Select(x => Map(x))
                .ToListAsync(cancellationToken);

            return Ok(new FormDefinitionListResponse { Items = items });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] string? companyNmlsNumber, [FromBody] CreateFormDefinitionRequest request, CancellationToken cancellationToken)
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

            if (request.OperationalAssetId.HasValue)
            {
                var assetExists = await Context.OperationalAssets.AnyAsync(x => x.Id == request.OperationalAssetId.Value && x.CompanyNmlsNumber == company, cancellationToken);
                if (!assetExists)
                    return NotFound("Operational asset not found.");
            }

            var form = new FormDefinition
            {
                CompanyNmlsNumber = company,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                FormType = request.FormType?.Trim(),
                Category = request.Category?.Trim(),
                Version = request.Version?.Trim(),
                OperationalAssetId = request.OperationalAssetId,
                IsActive = request.IsActive,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedByUserId = userId
            };

            Context.FormDefinitions.Add(form);
            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "FormCreated", "Forms", new { form.Id, form.Name }, cancellationToken);

            return Ok(Map(form));
        }

        [HttpPut("{formId:int}")]
        public async Task<IActionResult> Update(int formId, [FromQuery] string? companyNmlsNumber, [FromBody] UpdateFormDefinitionRequest request, CancellationToken cancellationToken)
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

            var form = await Context.FormDefinitions.FirstOrDefaultAsync(x => x.Id == formId && x.CompanyNmlsNumber == company, cancellationToken);
            if (form == null)
                return NotFound("Form definition not found.");

            if (request.OperationalAssetId.HasValue)
            {
                var assetExists = await Context.OperationalAssets.AnyAsync(x => x.Id == request.OperationalAssetId.Value && x.CompanyNmlsNumber == company, cancellationToken);
                if (!assetExists)
                    return NotFound("Operational asset not found.");
            }

            form.Name = request.Name.Trim();
            form.Description = request.Description?.Trim();
            form.FormType = request.FormType?.Trim();
            form.Category = request.Category?.Trim();
            form.Version = request.Version?.Trim();
            form.OperationalAssetId = request.OperationalAssetId;
            form.IsActive = request.IsActive;
            form.UpdatedAtUtc = DateTime.UtcNow;
            form.UpdatedByUserId = userId;

            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "FormUpdated", "Forms", new { form.Id, form.Name, form.IsActive }, cancellationToken);

            return Ok(Map(form));
        }

        [HttpDelete("{formId:int}")]
        public async Task<IActionResult> Delete(int formId, [FromQuery] string? companyNmlsNumber, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out var currentCompanyNmls, out var role))
                return Unauthorized();

            if (!AdminRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                return Forbid();

            var company = ResolveCompanyScope(companyNmlsNumber, currentCompanyNmls);
            if (company == null)
                return Unauthorized();

            var form = await Context.FormDefinitions.FirstOrDefaultAsync(x => x.Id == formId && x.CompanyNmlsNumber == company, cancellationToken);
            if (form == null)
                return NotFound("Form definition not found.");

            form.IsActive = false;
            form.UpdatedAtUtc = DateTime.UtcNow;
            form.UpdatedByUserId = userId;

            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "FormDeleted", "Forms", new { form.Id, form.Name }, cancellationToken);

            return Ok(new { message = "Form deactivated." });
        }

        private static FormDefinitionResponse Map(FormDefinition form)
        {
            return new FormDefinitionResponse
            {
                Id = form.Id,
                CompanyNmlsNumber = form.CompanyNmlsNumber,
                Name = form.Name,
                Description = form.Description,
                FormType = form.FormType,
                Category = form.Category,
                Version = form.Version,
                OperationalAssetId = form.OperationalAssetId,
                IsActive = form.IsActive,
                CreatedAtUtc = form.CreatedAtUtc,
                UpdatedAtUtc = form.UpdatedAtUtc
            };
        }
    }
}