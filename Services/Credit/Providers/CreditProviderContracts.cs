using PrestexaAPI.Models;

namespace PrestexaAPI.Services.Credit.Providers
{
    public class CreditProviderOrderRequest
    {
        public string LoanNumber { get; init; } = string.Empty;
        public CreditReportType ReportType { get; init; }
        public int? BorrowerId { get; init; }
        public int? CoBorrowerId { get; init; }
        public string RequestedBy { get; init; } = string.Empty;
    }

    public class CreditProviderOrderResult
    {
        public CreditReportStatus Status { get; init; }
        public int? TransUnionScore { get; init; }
        public int? EquifaxScore { get; init; }
        public int? ExperianScore { get; init; }
        public int? MiddleScore { get; init; }
        public string RawDataContent { get; init; } = string.Empty;
        public string XmlContent { get; init; } = string.Empty;
        public byte[] PdfBytes { get; init; } = Array.Empty<byte>();
    }
}
