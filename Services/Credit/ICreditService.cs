using PrestexaAPI.Models.Requests;
using PrestexaAPI.Models.Responses;

namespace PrestexaAPI.Services.Credit
{
    public interface ICreditService
    {
        Task<CreditReportResponse> OrderAsync(OrderCreditReportRequest request, CreditUserContext user, CancellationToken cancellationToken);
        Task<IReadOnlyList<CreditReportResponse>> GetReportsAsync(string loanId, CreditUserContext user, CancellationToken cancellationToken);
    }
}
