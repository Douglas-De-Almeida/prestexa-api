using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class LoanMilestone
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
        public string MilestoneType { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string MilestoneStatus { get; set; } = "Pending";

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
