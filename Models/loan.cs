using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class Loan
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        [Required]
        [StringLength(8)]
        public string LoanNumber { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Subject_Street_Address { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Subject_City { get; set; } = null!;

        [Required]
        [MaxLength(2)]
        public string Subject_State { get; set; } = null!;

        [Required]
        [MaxLength(10)]
        public string Subject_ZipCode { get; set; } = null!;

        [Range(1, 100000000)]
        public decimal LoanAmount { get; set; }

        public LoanStatus Status { get; set; } = LoanStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}