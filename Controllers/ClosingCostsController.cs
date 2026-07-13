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
    [Route("api/settings/closing-costs")]
    public class ClosingCostsController : AdminSettingsControllerBase
    {
        public ClosingCostsController(AppDbContext context) : base(context)
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

            var items = await Context.ClosingCostItems
                .AsNoTracking()
                .Where(x => x.CompanyNmlsNumber == company)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.FeeName)
                .Select(x => Map(x))
                .ToListAsync(cancellationToken);

            return Ok(new ClosingCostItemListResponse { Items = items });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] string? companyNmlsNumber, [FromBody] CreateClosingCostItemRequest request, CancellationToken cancellationToken)
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

            var item = new ClosingCostItem
            {
                CompanyNmlsNumber = company,
                FeeName = request.FeeName.Trim(),
                FeeCategory = request.FeeCategory?.Trim(),
                Amount = request.Amount,
                Percentage = request.Percentage,
                PaidBy = request.PaidBy?.Trim(),
                IsFinanceCharge = request.IsFinanceCharge,
                IsAprFee = request.IsAprFee,
                StateApplicability = request.StateApplicability?.Trim(),
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedByUserId = userId
            };

            Context.ClosingCostItems.Add(item);
            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "ClosingCostCreated", "ClosingCosts", new { item.Id, item.FeeName }, cancellationToken);

            return Ok(Map(item));
        }

        [HttpPut("{costId:int}")]
        public async Task<IActionResult> Update(int costId, [FromQuery] string? companyNmlsNumber, [FromBody] UpdateClosingCostItemRequest request, CancellationToken cancellationToken)
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

            var item = await Context.ClosingCostItems.FirstOrDefaultAsync(x => x.Id == costId && x.CompanyNmlsNumber == company, cancellationToken);
            if (item == null)
                return NotFound("Closing cost item not found.");

            item.FeeName = request.FeeName.Trim();
            item.FeeCategory = request.FeeCategory?.Trim();
            item.Amount = request.Amount;
            item.Percentage = request.Percentage;
            item.PaidBy = request.PaidBy?.Trim();
            item.IsFinanceCharge = request.IsFinanceCharge;
            item.IsAprFee = request.IsAprFee;
            item.StateApplicability = request.StateApplicability?.Trim();
            item.DisplayOrder = request.DisplayOrder;
            item.IsActive = request.IsActive;
            item.UpdatedAtUtc = DateTime.UtcNow;
            item.UpdatedByUserId = userId;

            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "ClosingCostUpdated", "ClosingCosts", new { item.Id, item.FeeName, item.IsActive }, cancellationToken);

            return Ok(Map(item));
        }

        [HttpDelete("{costId:int}")]
        public async Task<IActionResult> Delete(int costId, [FromQuery] string? companyNmlsNumber, CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUser(out var userId, out var currentCompanyNmls, out var role))
                return Unauthorized();

            if (!AdminRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                return Forbid();

            var company = ResolveCompanyScope(companyNmlsNumber, currentCompanyNmls);
            if (company == null)
                return Unauthorized();

            var item = await Context.ClosingCostItems.FirstOrDefaultAsync(x => x.Id == costId && x.CompanyNmlsNumber == company, cancellationToken);
            if (item == null)
                return NotFound("Closing cost item not found.");

            item.IsActive = false;
            item.UpdatedAtUtc = DateTime.UtcNow;
            item.UpdatedByUserId = userId;

            await Context.SaveChangesAsync(cancellationToken);
            await AddAuditAsync(company, userId, "ClosingCostDeleted", "ClosingCosts", new { item.Id, item.FeeName }, cancellationToken);

            return Ok(new { message = "Closing cost item disabled." });
        }

        private static ClosingCostItemResponse Map(ClosingCostItem item)
        {
            return new ClosingCostItemResponse
            {
                Id = item.Id,
                CompanyNmlsNumber = item.CompanyNmlsNumber,
                FeeName = item.FeeName,
                FeeCategory = item.FeeCategory,
                Amount = item.Amount,
                Percentage = item.Percentage,
                PaidBy = item.PaidBy,
                IsFinanceCharge = item.IsFinanceCharge,
                IsAprFee = item.IsAprFee,
                StateApplicability = item.StateApplicability,
                DisplayOrder = item.DisplayOrder,
                IsActive = item.IsActive,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            };
        }
    }
}