using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class BorrowerDeclaration
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

        [Required]
        [MaxLength(100)]
        public string DeclarationType { get; set; } = null!;

        public bool IsAffirmative { get; set; }

        [MaxLength(1000)]
        public string? Explanation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
