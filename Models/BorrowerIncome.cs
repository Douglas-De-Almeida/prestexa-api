using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class BorrowerIncome
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int BorrowerId { get; set; }

        public Borrower Borrower { get; set; } = null!;

        public int? BorrowerEmploymentId { get; set; }

        public BorrowerEmployment? BorrowerEmployment { get; set; }

        [Required]
        [MaxLength(50)]
        public string IncomeType { get; set; } = null!;

        [Range(0, 100000000)]
        public decimal CurrentIncomeMonthlyTotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
