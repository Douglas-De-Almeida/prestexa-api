using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class BorrowerEmployment
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int BorrowerId { get; set; }

        public Borrower Borrower { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string EmployerName { get; set; } = null!;

        [MaxLength(50)]
        public string EmploymentStatusType { get; set; } = "Current";

        public DateTime? EmploymentStartDate { get; set; }

        [MaxLength(100)]
        public string? EmploymentPositionDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
