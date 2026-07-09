using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class LoanTerms
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        [Range(1, 100000000)]
        public decimal NoteAmount { get; set; }

        [Range(0, 100)]
        public decimal NoteRatePercent { get; set; }

        [Range(1, 480)]
        public int LoanAmortizationPeriodCount { get; set; }

        [MaxLength(50)]
        public string LoanAmortizationType { get; set; } = "Fixed";

        [MaxLength(50)]
        public string LoanPurposeType { get; set; } = "Purchase";

        [MaxLength(50)]
        public string MortgageType { get; set; } = "Conventional";

        [MaxLength(50)]
        public string LienPriorityType { get; set; } = "FirstLien";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
