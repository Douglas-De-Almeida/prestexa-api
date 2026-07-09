using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class LoanCondition
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ConditionCategory { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string ConditionStatus { get; set; } = "Open";

        [MaxLength(1000)]
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
