using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class UpdateBorrowerRequest
    {
        [Required]
        [MaxLength(50)]
        public string BorrowerType { get; set; } = "PrimaryBorrower";

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? CellPhone { get; set; }

        [MaxLength(30)]
        public string? HomePhone { get; set; }

        [MaxLength(30)]
        public string? WorkPhone { get; set; }

        [MaxLength(20)]
        public string? MaritalStatus { get; set; }

        [MaxLength(100)]
        public string? ResidencyType { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public int? EstimatedCreditScore { get; set; }

        public bool EConsentAuthorized { get; set; }

        public bool CreditPullAuthorized { get; set; }
    }
}