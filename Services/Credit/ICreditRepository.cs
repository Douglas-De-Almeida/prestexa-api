using PrestexaAPI.Models;

namespace PrestexaAPI.Services.Credit
{
    public interface ICreditRepository
    {
        Task<Loan?> FindLoanForAccessAsync(string loanNumber, CreditUserContext user, CancellationToken cancellationToken);
        Task<Loan?> FindLoanByIdForAccessAsync(int loanId, CreditUserContext user, CancellationToken cancellationToken);
        Task<bool> HasConsentAsync(int loanId, int? borrowerId, int? coBorrowerId, CancellationToken cancellationToken);
        Task AddCreditReportAsync(CreditReport report, CancellationToken cancellationToken);
        Task<List<CreditReport>> GetReportsForLoanAsync(int loanId, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
