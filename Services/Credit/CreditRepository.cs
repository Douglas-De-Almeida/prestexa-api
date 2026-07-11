using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;

namespace PrestexaAPI.Services.Credit
{
    public class CreditRepository : ICreditRepository
    {
        private readonly AppDbContext _context;

        public CreditRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Loan?> FindLoanForAccessAsync(string loanNumber, CreditUserContext user, CancellationToken cancellationToken)
        {
            var normalizedLoanNumber = NormalizeLoanNumber(loanNumber);

            var query = BuildLoanAccessQuery(user)
                .Where(l => l.LoanNumber == normalizedLoanNumber);

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Loan?> FindLoanByIdForAccessAsync(int loanId, CreditUserContext user, CancellationToken cancellationToken)
        {
            var query = BuildLoanAccessQuery(user)
                .Where(l => l.Id == loanId);

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> HasConsentAsync(int loanId, int? borrowerId, int? coBorrowerId, CancellationToken cancellationToken)
        {
            var borrowers = _context.Borrowers
                .Where(b => b.LoanId == loanId);

            if (borrowerId.HasValue)
                borrowers = borrowers.Where(b => b.Id == borrowerId.Value || (coBorrowerId.HasValue && b.Id == coBorrowerId.Value));

            if (!borrowerId.HasValue && coBorrowerId.HasValue)
                borrowers = borrowers.Where(b => b.Id == coBorrowerId.Value);

            return await borrowers.AnyAsync(b => b.CreditPullAuthorized, cancellationToken);
        }

        public async Task AddCreditReportAsync(CreditReport report, CancellationToken cancellationToken)
        {
            await _context.CreditReports.AddAsync(report, cancellationToken);
        }

        public async Task<List<CreditReport>> GetReportsForLoanAsync(int loanId, CancellationToken cancellationToken)
        {
            return await _context.CreditReports
                .AsNoTracking()
                .Where(r => r.LoanId == loanId)
                .OrderByDescending(r => r.OrderedAtUtc)
                .ThenByDescending(r => r.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        private IQueryable<Loan> BuildLoanAccessQuery(CreditUserContext user)
        {
            var query = _context.Loans.AsQueryable();

            if (!user.IsSuperAdmin)
            {
                if (string.IsNullOrWhiteSpace(user.CompanyNmlsNumber))
                    return query.Where(l => false);

                query = query.Where(l => l.CompanyNmlsNumber == user.CompanyNmlsNumber);
            }

            if (user.IsLoanOfficer)
                query = query.Where(l => l.UserId == user.UserId);

            return query;
        }

        private static string NormalizeLoanNumber(string loanNumber)
        {
            var trimmed = loanNumber.Trim();

            if (trimmed.StartsWith("LN-", StringComparison.OrdinalIgnoreCase))
                return trimmed[3..];

            return trimmed;
        }
    }
}
