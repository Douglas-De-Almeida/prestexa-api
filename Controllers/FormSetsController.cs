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
    [Route("api/settings/form-sets")]
    public class FormSetsController : AdminSettingsControllerBase
    {
        public FormSetsController(AppDbContext context) : base(context)
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

            var items = await Context.FormSets
                .AsNoTracking()
                .Where(x => x.CompanyNmlsNumber == company)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

            var setIds = items.Select(x => x.Id).ToList();
            var setItems = await Context.FormSetItems
                .AsNoTracking()
                .Where(x => setIds.Contains(x.FormSetId))
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);

            var formIds = setItems.Select(x => x.FormDefinitionId).Distinct().ToList();
            var formLookup = await Context.FormDefinitions
                .AsNoTracking()
                .Where(x => x.CompanyNmlsNumber == company && formIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

            var responses = items.Select(set => Map(set, setItems.Where(x => x.FormSetId == set.Id).ToList(), formLookup)).ToList();
            return Ok(new FormSetListResponse { Items = responses });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] string? companyNmlsNumber, [FromBody] CreateFormSetRequest request, CancellationToken cancellationToken)
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

            var formSet = new FormSet
            {
                CompanyNmlsNumber = company,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                IsActive = request.IsActive,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedByUserId = userId
            };

            var formIds = request.Items.Select(x => x.FormDefinitionId).Distinct().ToList();
            var validForms = await Context.FormDefinitions
                .AsNoTracking()
                .Where(x => x.CompanyNmlsNumber == company && formIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (validForms.Count != formIds.Count)
                return NotFound("One or more form definitions were not found for this company.");

            Context.FormSets.Add(formSet);
            await Context.SaveChangesAsync(cancellationToken);

            foreach (var item in request.Items.OrderBy(x => x.DisplayOrder).Select((item, index) => new FormSetItem
            {
                FormSetId = formSet.Id,
                FormDefinitionId = item.FormDefinitionId,
                DisplayOrder = item.DisplayOrder == 0 ? index + 1 : item.DisplayOrder
            }))
            {
                Context.FormSetItems.Add(item);
            }

            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "FormSetCreated", "FormSets", new { formSet.Id, formSet.Name, itemCount = request.Items.Count }, cancellationToken);

            return Ok(await LoadSetAsync(formSet.Id, company, cancellationToken));
        }

        [HttpPut("{formSetId:int}")]
        public async Task<IActionResult> Update(int formSetId, [FromQuery] string? companyNmlsNumber, [FromBody] UpdateFormSetRequest request, CancellationToken cancellationToken)
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

            var formSet = await Context.FormSets.FirstOrDefaultAsync(x => x.Id == formSetId && x.CompanyNmlsNumber == company, cancellationToken);
            if (formSet == null)
                return NotFound("Form set not found.");

            var formIds = request.Items.Select(x => x.FormDefinitionId).Distinct().ToList();
            var validForms = await Context.FormDefinitions
                .AsNoTracking()
                .Where(x => x.CompanyNmlsNumber == company && formIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (validForms.Count != formIds.Count)
                return NotFound("One or more form definitions were not found for this company.");

            formSet.Name = request.Name.Trim();
            formSet.Description = request.Description?.Trim();
            formSet.IsActive = request.IsActive;
            formSet.UpdatedAtUtc = DateTime.UtcNow;
            formSet.UpdatedByUserId = userId;

            var existingItems = Context.FormSetItems.Where(x => x.FormSetId == formSet.Id);
            Context.FormSetItems.RemoveRange(existingItems);

            await Context.SaveChangesAsync(cancellationToken);

            foreach (var item in request.Items.OrderBy(x => x.DisplayOrder).Select((item, index) => new FormSetItem
            {
                FormSetId = formSet.Id,
                FormDefinitionId = item.FormDefinitionId,
                DisplayOrder = item.DisplayOrder == 0 ? index + 1 : item.DisplayOrder
            }))
            {
                Context.FormSetItems.Add(item);
            }

            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "FormSetUpdated", "FormSets", new { formSet.Id, formSet.Name, itemCount = request.Items.Count }, cancellationToken);

            return Ok(await LoadSetAsync(formSet.Id, company, cancellationToken));
        }

        [HttpDelete("{formSetId:int}")]
        public async Task<IActionResult> Delete(int formSetId, [FromQuery] string? companyNmlsNumber, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out var currentCompanyNmls, out var role))
                return Unauthorized();

            if (!AdminRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                return Forbid();

            var company = ResolveCompanyScope(companyNmlsNumber, currentCompanyNmls);
            if (company == null)
                return Unauthorized();

            var formSet = await Context.FormSets.FirstOrDefaultAsync(x => x.Id == formSetId && x.CompanyNmlsNumber == company, cancellationToken);
            if (formSet == null)
                return NotFound("Form set not found.");

            formSet.IsActive = false;
            formSet.UpdatedAtUtc = DateTime.UtcNow;
            formSet.UpdatedByUserId = userId;

            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "FormSetDeleted", "FormSets", new { formSet.Id, formSet.Name }, cancellationToken);

            return Ok(new { message = "Form set deactivated." });
        }

        private async Task<FormSetResponse> LoadSetAsync(int formSetId, string company, CancellationToken cancellationToken)
        {
            var set = await Context.FormSets.AsNoTracking().FirstAsync(x => x.Id == formSetId && x.CompanyNmlsNumber == company, cancellationToken);
            var items = await Context.FormSetItems.AsNoTracking()
                .Where(x => x.FormSetId == set.Id)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);

            var formLookup = await Context.FormDefinitions.AsNoTracking()
                .Where(x => x.CompanyNmlsNumber == company && items.Select(i => i.FormDefinitionId).Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

            return Map(set, items, formLookup);
        }

        private static FormSetResponse Map(FormSet set, IReadOnlyList<FormSetItem> items, IReadOnlyDictionary<int, string> formLookup)
        {
            return new FormSetResponse
            {
                Id = set.Id,
                CompanyNmlsNumber = set.CompanyNmlsNumber,
                Name = set.Name,
                Description = set.Description,
                IsActive = set.IsActive,
                CreatedAtUtc = set.CreatedAtUtc,
                UpdatedAtUtc = set.UpdatedAtUtc,
                Items = items.Select(item => new FormSetItemResponse
                {
                    Id = item.Id,
                    FormSetId = item.FormSetId,
                    FormDefinitionId = item.FormDefinitionId,
                    DisplayOrder = item.DisplayOrder
                }).ToList()
            };
        }
    }
}