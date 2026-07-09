using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class LoanTask
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        public int? LoanConditionId { get; set; }

        public LoanCondition? LoanCondition { get; set; }

        public int? AssignedUserId { get; set; }

        public User? AssignedUser { get; set; }

        [Required]
        [MaxLength(100)]
        public string TaskType { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string TaskStatus { get; set; } = "Open";

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
