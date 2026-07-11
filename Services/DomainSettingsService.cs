using Microsoft.EntityFrameworkCore;
using Npgsql;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;
using PrestexaAPI.Models.Responses;

namespace PrestexaAPI.Services
{
    public class DomainSettingsService : IDomainSettingsService
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DomainSettingsService(
            AppDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<DomainSettingsResponse?> GetCurrentAsync(CancellationToken cancellationToken)
        {
            var company = await GetCurrentCompanyAsync(cancellationToken);

            var domain = await _context.CompanyDomains
                .AsNoTracking()
                .Where(x => x.CompanyId == company.Id && x.IsActive)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            return domain == null
                ? null
                : MapDomainResponse(domain);
        }

        public async Task<DomainSettingsResponse> UpsertCurrentAsync(
            UpdateDomainSettingsRequest request,
            CancellationToken cancellationToken)
        {
            var company = await GetCurrentCompanyAsync(cancellationToken);

            var normalizedSubdomain = ValidateAndNormalizeSubdomain(request.Subdomain);

            var duplicateExists = await _context.CompanyDomains
                .AnyAsync(
                    x => x.Subdomain == normalizedSubdomain &&
                         x.CompanyId != company.Id,
                    cancellationToken);

            if (duplicateExists)
            {
                throw new DomainConflictException("Subdomain already in use.");
            }

            var now = DateTime.UtcNow;

            var domain = await _context.CompanyDomains
                .Where(x => x.CompanyId == company.Id && x.IsActive)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var auditEntries = new List<OrganizationAuditRecord>();

            if (domain == null)
            {
                domain = new CompanyDomain
                {
                    CompanyId = company.Id,
                    Subdomain = normalizedSubdomain,
                    IsActive = true,
                    IsVerified = true,
                    CreatedAt = now,
                    CreatedByUserId = _currentUser.UserId
                };

                _context.CompanyDomains.Add(domain);

                auditEntries.Add(new OrganizationAuditRecord
                {
                    OrganizationId = company.Id,
                    CompanyNmlsNumber = company.NmlsNumber,
                    Action = "Domain Created",
                    FieldName = "Subdomain",
                    OldValue = null,
                    NewValue = normalizedSubdomain,
                    ChangedByUserId = _currentUser.UserId,
                    ChangedAtUtc = now
                });

                auditEntries.Add(new OrganizationAuditRecord
                {
                    OrganizationId = company.Id,
                    CompanyNmlsNumber = company.NmlsNumber,
                    Action = "Domain Verified",
                    FieldName = "IsVerified",
                    OldValue = "False",
                    NewValue = "True",
                    ChangedByUserId = _currentUser.UserId,
                    ChangedAtUtc = now
                });
            }
            else
            {
                if (!string.Equals(domain.Subdomain, normalizedSubdomain, StringComparison.Ordinal))
                {
                    auditEntries.Add(new OrganizationAuditRecord
                    {
                        OrganizationId = company.Id,
                        CompanyNmlsNumber = company.NmlsNumber,
                        Action = "Domain Updated",
                        FieldName = "Subdomain",
                        OldValue = domain.Subdomain,
                        NewValue = normalizedSubdomain,
                        ChangedByUserId = _currentUser.UserId,
                        ChangedAtUtc = now
                    });

                    domain.Subdomain = normalizedSubdomain;
                }

                if (!domain.IsVerified)
                {
                    auditEntries.Add(new OrganizationAuditRecord
                    {
                        OrganizationId = company.Id,
                        CompanyNmlsNumber = company.NmlsNumber,
                        Action = "Domain Verified",
                        FieldName = "IsVerified",
                        OldValue = "False",
                        NewValue = "True",
                        ChangedByUserId = _currentUser.UserId,
                        ChangedAtUtc = now
                    });
                }

                domain.IsVerified = true;
                domain.UpdatedAt = now;
                domain.UpdatedByUserId = _currentUser.UserId;
            }

            if (auditEntries.Count > 0)
            {
                _context.OrganizationAuditRecords.AddRange(auditEntries);
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsSubdomainUniqueViolation(ex))
            {
                throw new DomainConflictException("Subdomain already in use.");
            }

            return MapDomainResponse(domain);
        }

        private async Task<Company> GetCurrentCompanyAsync(CancellationToken cancellationToken)
        {
            if (string.Equals(_currentUser.Role, "Borrower", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_currentUser.Role, "REA", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Domain settings are only available to LOS users.");
            }

            if (string.IsNullOrWhiteSpace(_currentUser.CompanyNmlsNumber))
            {
                throw new UnauthorizedAccessException("CompanyNmlsNumber claim is required.");
            }

            var company = await _context.Companies
                .FirstOrDefaultAsync(x => x.NmlsNumber == _currentUser.CompanyNmlsNumber, cancellationToken);

            if (company == null)
            {
                throw new KeyNotFoundException("Company not found.");
            }

            return company;
        }

        private static string ValidateAndNormalizeSubdomain(string subdomain)
        {
            if (string.IsNullOrWhiteSpace(subdomain))
            {
                throw new ArgumentException("Subdomain is required.", nameof(subdomain));
            }

            var normalized = subdomain.Trim();

            if (!string.Equals(normalized, normalized.ToLowerInvariant(), StringComparison.Ordinal))
            {
                throw new ArgumentException("Subdomain must be lowercase.", nameof(subdomain));
            }

            if (normalized.Length > 100)
            {
                throw new ArgumentException("Subdomain is too long.", nameof(subdomain));
            }

            if (DomainValidationRules.IsReservedSubdomain(normalized))
            {
                throw new ArgumentException("Subdomain is reserved.", nameof(subdomain));
            }

            if (!DomainValidationRules.IsValidSubdomainFormat(normalized))
            {
                throw new ArgumentException("Subdomain must be URL-safe and may only contain lowercase letters, numbers, and hyphens.", nameof(subdomain));
            }

            return normalized;
        }

        private static DomainSettingsResponse MapDomainResponse(CompanyDomain domain)
        {
            return new DomainSettingsResponse
            {
                Subdomain = domain.Subdomain,
                FullUrl = $"https://{domain.Subdomain}.prestexa.com",
                IsVerified = domain.IsVerified
            };
        }

        private static bool IsSubdomainUniqueViolation(DbUpdateException ex)
        {
            return ex.InnerException is PostgresException postgresException &&
                   postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                   string.Equals(postgresException.ConstraintName, "IX_CompanyDomains_Subdomain", StringComparison.Ordinal);
        }
    }

    public sealed class DomainConflictException : Exception
    {
        public DomainConflictException(string message) : base(message)
        {
        }
    }
}
