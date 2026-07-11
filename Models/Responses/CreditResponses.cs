namespace PrestexaAPI.Models.Responses
{
    public class CreditReportResponse
    {
        public int ReportId { get; set; }
        public int LoanId { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime OrderedAtUtc { get; set; }
        public int? TransUnionScore { get; set; }
        public int? EquifaxScore { get; set; }
        public int? ExperianScore { get; set; }
        public int? MiddleScore { get; set; }
        public string? RawDataLocation { get; set; }
        public string? XmlFileLocation { get; set; }
        public string? PdfFileLocation { get; set; }
    }
}
