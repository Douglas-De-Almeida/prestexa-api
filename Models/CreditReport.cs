using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class CreditReport
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        public int? BorrowerId { get; set; }

        public Borrower? Borrower { get; set; }

        public int? CoBorrowerId { get; set; }

        public Borrower? CoBorrower { get; set; }

        [Required]
        [MaxLength(100)]
        public string Provider { get; set; } = null!;

        public CreditReportType ReportType { get; set; } = CreditReportType.TriMerge;

        public CreditReportStatus Status { get; set; } = CreditReportStatus.Ordered;

        public int OrderedByUserId { get; set; }

        public User OrderedByUser { get; set; } = null!;

        public DateTime OrderedAtUtc { get; set; } = DateTime.UtcNow;

        public int? TransUnionScore { get; set; }

        public int? EquifaxScore { get; set; }

        public int? ExperianScore { get; set; }

        public int? MiddleScore { get; set; }

        [MaxLength(1000)]
        public string? RawDataLocation { get; set; }

        [MaxLength(1000)]
        public string? XmlFileLocation { get; set; }

        [MaxLength(1000)]
        public string? PdfFileLocation { get; set; }
    }
}
