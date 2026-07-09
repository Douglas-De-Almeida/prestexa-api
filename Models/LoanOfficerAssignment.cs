using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class LoanOfficerAssignment
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public bool IsPrimary { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
