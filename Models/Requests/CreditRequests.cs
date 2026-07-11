using System.ComponentModel.DataAnnotations;
using PrestexaAPI.Models;

namespace PrestexaAPI.Models.Requests
{
    public class OrderCreditReportRequest
    {
        [Required]
        [MaxLength(20)]
        public string LoanNumber { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Provider { get; set; } = null!;

        public CreditReportType ReportType { get; set; } = CreditReportType.TriMerge;

        public int? BorrowerId { get; set; }

        public int? CoBorrowerId { get; set; }

        public bool ConsentAcknowledged { get; set; }
    }
}
