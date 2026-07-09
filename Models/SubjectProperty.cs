using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class SubjectProperty
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string AddressLineText { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string CityName { get; set; } = null!;

        [Required]
        [MaxLength(2)]
        public string StateCode { get; set; } = null!;

        [Required]
        [MaxLength(10)]
        public string PostalCode { get; set; } = null!;

        [MaxLength(50)]
        public string PropertyUsageType { get; set; } = "PrimaryResidence";

        [Range(1, 8)]
        public int FinancedUnitCount { get; set; } = 1;

        [Range(1, 100000000)]
        public decimal PropertyEstimatedValueAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
