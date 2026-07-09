using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class LoanRealtorAssignment
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        public int RealtorId { get; set; }

        public Realtor Realtor { get; set; } = null!;

        [MaxLength(50)]
        public string AssignmentType { get; set; } = "ListingAgent";

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
