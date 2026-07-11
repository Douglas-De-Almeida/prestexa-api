using PrestexaAPI.Models;

namespace PrestexaAPI.Services.Credit.Providers
{
    public abstract class BaseCreditProviderAdapter : ICreditProviderAdapter
    {
        public abstract string ProviderKey { get; }

        public Task<CreditProviderOrderResult> OrderAsync(CreditProviderOrderRequest request, CancellationToken cancellationToken)
        {
            var seed = Math.Abs(HashCode.Combine(ProviderKey, request.LoanNumber, request.BorrowerId, request.CoBorrowerId));

            var tu = 620 + (seed % 120);
            var eq = 620 + ((seed / 3) % 120);
            var ex = 620 + ((seed / 7) % 120);

            var orderedScores = new[] { tu, eq, ex }.OrderBy(x => x).ToArray();
            var middle = orderedScores[1];

            var rawData =
                "{" +
                $"\"provider\":\"{ProviderKey}\"," +
                $"\"loanNumber\":\"{request.LoanNumber}\"," +
                $"\"reportType\":\"{request.ReportType}\"," +
                $"\"requestedBy\":\"{request.RequestedBy}\"," +
                $"\"scores\":{{\"tu\":{tu},\"eq\":{eq},\"ex\":{ex},\"middle\":{middle}}}" +
                "}";

            var xml =
                $"<CreditReport provider=\"{ProviderKey}\" loanNumber=\"{request.LoanNumber}\" reportType=\"{request.ReportType}\">" +
                $"<Scores transUnion=\"{tu}\" equifax=\"{eq}\" experian=\"{ex}\" middle=\"{middle}\" />" +
                "</CreditReport>";

            var pdfContent = $"Credit Report ({ProviderKey}) for Loan {request.LoanNumber}\nTU={tu} EQ={eq} EX={ex} Middle={middle}";
            var pdfBytes = System.Text.Encoding.UTF8.GetBytes(pdfContent);

            return Task.FromResult(new CreditProviderOrderResult
            {
                Status = CreditReportStatus.Completed,
                TransUnionScore = tu,
                EquifaxScore = eq,
                ExperianScore = ex,
                MiddleScore = middle,
                RawDataContent = rawData,
                XmlContent = xml,
                PdfBytes = pdfBytes
            });
        }
    }
}
