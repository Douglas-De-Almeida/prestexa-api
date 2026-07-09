using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class GovernmentMonitoring
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        public int BorrowerId { get; set; }

        public Borrower Borrower { get; set; } = null!;

        [MaxLength(100)]
        public string? EthnicityType { get; set; }

        [MaxLength(100)]
        public string? RaceType { get; set; }

        [MaxLength(100)]
        public string? SexType { get; set; }

        [MaxLength(100)]
        public string? CollectionMethodType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
