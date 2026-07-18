using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;
using PrestexaAPI.Models.Responses;

namespace PrestexaAPI.Services
{
    public class OrganizationSettingsService : IOrganizationSettingsService
    {
        private static readonly Dictionary<string, string> StateMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AL"] = "AL", ["Alabama"] = "AL",
            ["AK"] = "AK", ["Alaska"] = "AK",
            ["AZ"] = "AZ", ["Arizona"] = "AZ",
            ["AR"] = "AR", ["Arkansas"] = "AR",
            ["CA"] = "CA", ["California"] = "CA",
            ["CO"] = "CO", ["Colorado"] = "CO",
            ["CT"] = "CT", ["Connecticut"] = "CT",
            ["DE"] = "DE", ["Delaware"] = "DE",
            ["FL"] = "FL", ["Florida"] = "FL",
            ["GA"] = "GA", ["Georgia"] = "GA",
            ["HI"] = "HI", ["Hawaii"] = "HI",
            ["ID"] = "ID", ["Idaho"] = "ID",
            ["IL"] = "IL", ["Illinois"] = "IL",
            ["IN"] = "IN", ["Indiana"] = "IN",
            ["IA"] = "IA", ["Iowa"] = "IA",
            ["KS"] = "KS", ["Kansas"] = "KS",
            ["KY"] = "KY", ["Kentucky"] = "KY",
            ["LA"] = "LA", ["Louisiana"] = "LA",
            ["ME"] = "ME", ["Maine"] = "ME",
            ["MD"] = "MD", ["Maryland"] = "MD",
            ["MA"] = "MA", ["Massachusetts"] = "MA",
            ["MI"] = "MI", ["Michigan"] = "MI",
            ["MN"] = "MN", ["Minnesota"] = "MN",
            ["MS"] = "MS", ["Mississippi"] = "MS",
            ["MO"] = "MO", ["Missouri"] = "MO",
            ["MT"] = "MT", ["Montana"] = "MT",
            ["NE"] = "NE", ["Nebraska"] = "NE",
            ["NV"] = "NV", ["Nevada"] = "NV",
            ["NH"] = "NH", ["New Hampshire"] = "NH",
            ["NJ"] = "NJ", ["New Jersey"] = "NJ",
            ["NM"] = "NM", ["New Mexico"] = "NM",
            ["NY"] = "NY", ["New York"] = "NY",
            ["NC"] = "NC", ["North Carolina"] = "NC",
            ["ND"] = "ND", ["North Dakota"] = "ND",
            ["OH"] = "OH", ["Ohio"] = "OH",
            ["OK"] = "OK", ["Oklahoma"] = "OK",
            ["OR"] = "OR", ["Oregon"] = "OR",
            ["PA"] = "PA", ["Pennsylvania"] = "PA",
            ["RI"] = "RI", ["Rhode Island"] = "RI",
            ["SC"] = "SC", ["South Carolina"] = "SC",
            ["SD"] = "SD", ["South Dakota"] = "SD",
            ["TN"] = "TN", ["Tennessee"] = "TN",
            ["TX"] = "TX", ["Texas"] = "TX",
            ["UT"] = "UT", ["Utah"] = "UT",
            ["VT"] = "VT", ["Vermont"] = "VT",
            ["VA"] = "VA", ["Virginia"] = "VA",
            ["WA"] = "WA", ["Washington"] = "WA",
            ["WV"] = "WV", ["West Virginia"] = "WV",
            ["WI"] = "WI", ["Wisconsin"] = "WI",
            ["WY"] = "WY", ["Wyoming"] = "WY"
        };

        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public OrganizationSettingsService(
            AppDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<OrganizationSettingsResponse?> GetCurrentAsync(CancellationToken cancellationToken)
        {
            var company = await GetCurrentCompanyQuery()
                .FirstOrDefaultAsync(cancellationToken);

            return company == null ? null : MapResponse(company);
        }

        public async Task<OrganizationSettingsResponse> UpdateCurrentAsync(
            UpdateOrganizationSettingsRequest request,
            CancellationToken cancellationToken)
        {
            var company = await GetCurrentCompanyQuery()
                .FirstOrDefaultAsync(cancellationToken);

            if (company == null)
            {
                throw new KeyNotFoundException("Organization not found.");
            }

            if (!string.IsNullOrWhiteSpace(request.CompanyNmlsId) &&
                !string.Equals(request.CompanyNmlsId.Trim(), company.NmlsNumber, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Company NMLS ID cannot be modified after organization creation.");
            }

            if (request.PosLoanAppAssigneeUserId.HasValue)
            {
                var assigneeExists = await _context.Users.AnyAsync(
                    u => u.Id == request.PosLoanAppAssigneeUserId.Value &&
                         u.CompanyNmlsNumber == company.NmlsNumber,
                    cancellationToken);

                if (!assigneeExists)
                {
                    throw new ArgumentException("POS Loan App Assignee must belong to the current organization.", nameof(request.PosLoanAppAssigneeUserId));
                }
            }

            var auditEntries = new List<OrganizationAuditRecord>();
            var changedAtUtc = DateTime.UtcNow;

            ApplyIfChanged(company, "Organization Name", company.Name, request.OrganizationName.Trim(), value => company.Name = value, auditEntries, changedAtUtc);
            ApplyIfChanged(company, "Street Address", company.StreetAddress, Normalize(request.StreetAddress), value => company.StreetAddress = value, auditEntries, changedAtUtc);
            ApplyIfChanged(company, "Apartment / Unit", company.AptUnit, Normalize(request.ApartmentUnit), value => company.AptUnit = value, auditEntries, changedAtUtc);
            ApplyIfChanged(company, "City", company.City, Normalize(request.City), value => company.City = value, auditEntries, changedAtUtc);
            ApplyIfChanged(company, "State", company.State, NormalizeState(request.State), value => company.State = value, auditEntries, changedAtUtc);
            ApplyIfChanged(company, "ZIP Code", company.ZipCode, Normalize(request.ZipCode), value => company.ZipCode = value, auditEntries, changedAtUtc);
            ApplyIfChanged(company, "Legal Entity Type", company.LegalEntityType, Normalize(request.LegalEntityType), value => company.LegalEntityType = value, auditEntries, changedAtUtc);
            ApplyIfChanged(company, "Legal Entity Organized Under The Laws Of", company.LegalEntityJurisdiction, Normalize(request.LegalEntityJurisdiction), value => company.LegalEntityJurisdiction = value, auditEntries, changedAtUtc);
            ApplyIfChanged(company, "Email", company.Email, Normalize(request.Email)?.ToLowerInvariant(), value => company.Email = value, auditEntries, changedAtUtc);
            ApplyIfChanged(company, "Phone Number", company.Phone, Normalize(request.Phone), value => company.Phone = value, auditEntries, changedAtUtc);
            ApplyIfChanged(company, "Website URL", company.WebsiteUrl, NormalizeWebsiteUrl(request.WebsiteUrl), value => company.WebsiteUrl = value, auditEntries, changedAtUtc);
            ApplyIfChanged(company, "POS Assignee", company.PosLoanAppAssigneeUserId, request.PosLoanAppAssigneeUserId, value => company.PosLoanAppAssigneeUserId = value, auditEntries, changedAtUtc);

            company.UpdatedAt = changedAtUtc;

            if (auditEntries.Count > 0)
            {
                _context.OrganizationAuditRecords.AddRange(auditEntries);
            }

            await _context.SaveChangesAsync(cancellationToken);

            var updatedCompany = await GetCurrentCompanyQuery()
                .FirstOrDefaultAsync(cancellationToken);

            return MapResponse(updatedCompany!);
        }

        public async Task<IReadOnlyList<OrganizationAssigneeOptionResponse>> GetAssigneeOptionsAsync(CancellationToken cancellationToken)
        {
            var companyNmlsNumber = GetRequiredCompanyNmlsNumber();

            return await _context.Users
                .Where(u => u.CompanyNmlsNumber == companyNmlsNumber)
                .Where(u => u.Role != "Borrower")
                .Where(u => u.Role != "REA")
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new OrganizationAssigneeOptionResponse
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    DisplayName = string.Join(" ", new[] { u.FirstName, u.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)))
                })
                .ToListAsync(cancellationToken);
        }

        private IQueryable<Company> GetCurrentCompanyQuery()
        {
            var companyNmlsNumber = GetRequiredCompanyNmlsNumber();

            return _context.Companies
                .Include(c => c.PosLoanAppAssigneeUser)
                .Where(c => c.NmlsNumber == companyNmlsNumber);
        }

        private string GetRequiredCompanyNmlsNumber()
        {
            if (string.Equals(_currentUser.Role, "Borrower", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_currentUser.Role, "REA", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Organization settings are only available to LOS users.");
            }

            if (string.IsNullOrWhiteSpace(_currentUser.CompanyNmlsNumber))
            {
                throw new UnauthorizedAccessException("CompanyNmlsNumber claim is required.");
            }

            return _currentUser.CompanyNmlsNumber;
        }

        private void ApplyIfChanged<T>(
            Company company,
            string auditFieldName,
            T currentValue,
            T newValue,
            Action<T> assign,
            ICollection<OrganizationAuditRecord> auditEntries,
            DateTime changedAtUtc)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
            {
                return;
            }

            assign(newValue);

            auditEntries.Add(new OrganizationAuditRecord
            {
                OrganizationId = company.Id,
                CompanyNmlsNumber = company.NmlsNumber,
                Action = $"{auditFieldName} Changed",
                FieldName = auditFieldName,
                OldValue = FormatAuditValue(currentValue),
                NewValue = FormatAuditValue(newValue),
                ChangedByUserId = _currentUser.UserId,
                ChangedAtUtc = changedAtUtc
            });
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string NormalizeState(string value)
        {
            var normalizedValue = value.Trim();

            if (StateMappings.TryGetValue(normalizedValue, out var stateCode))
            {
                return stateCode;
            }

            throw new ArgumentException("State must be a valid US state.", nameof(UpdateOrganizationSettingsRequest.State));
        }

        private static string? NormalizeWebsiteUrl(string? value)
        {
            var normalized = Normalize(value);

            if (normalized == null)
            {
                return null;
            }

            if (!normalized.Contains("://", StringComparison.Ordinal))
            {
                normalized = $"https://{normalized}";
            }

            if (!Uri.TryCreate(normalized, UriKind.Absolute, out _))
            {
                throw new ArgumentException("Website URL must be a valid URL.", nameof(UpdateOrganizationSettingsRequest.WebsiteUrl));
            }

            return normalized;
        }

        private static string? FormatAuditValue<T>(T value)
        {
            return value?.ToString();
        }

        private static OrganizationSettingsResponse MapResponse(Company company)
        {
            return new OrganizationSettingsResponse
            {
                Id = company.Id,
                OrganizationName = company.Name,
                CompanyNmlsId = company.NmlsNumber,
                StreetAddress = company.StreetAddress,
                ApartmentUnit = company.AptUnit,
                City = company.City,
                State = company.State,
                ZipCode = company.ZipCode,
                LegalEntityType = company.LegalEntityType,
                LegalEntityJurisdiction = company.LegalEntityJurisdiction,
                Email = company.Email,
                Phone = company.Phone,
                WebsiteUrl = company.WebsiteUrl,
                PosLoanAppAssignee = company.PosLoanAppAssigneeUser == null
                    ? null
                    : new OrganizationAssigneeOptionResponse
                    {
                        Id = company.PosLoanAppAssigneeUser.Id,
                        FirstName = company.PosLoanAppAssigneeUser.FirstName,
                        LastName = company.PosLoanAppAssigneeUser.LastName,
                        DisplayName = string.Join(" ", new[] { company.PosLoanAppAssigneeUser.FirstName, company.PosLoanAppAssigneeUser.LastName }.Where(x => !string.IsNullOrWhiteSpace(x)))
                    },
                CreatedAt = company.CreatedAt,
                UpdatedAt = company.UpdatedAt
            };
        }
    }
}