using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class HousingExpense
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

        [Required]
        [MaxLength(50)]
        public string HousingExpenseType { get; set; } = null!;

        [Range(0, 100000000)]
        public decimal PresentHousingExpenseAmount { get; set; }

        [Range(0, 100000000)]
        public decimal ProposedHousingExpenseAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
