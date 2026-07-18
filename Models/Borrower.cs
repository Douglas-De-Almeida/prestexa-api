using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class Borrower
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

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

        [MaxLength(20)]
        public string? Suffix { get; set; }

        [MaxLength(100)]
        public string? Nickname { get; set; }

        [MaxLength(20)]
        public string? SsnItin { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? CellPhone { get; set; }

        [MaxLength(30)]
        public string? HomePhone { get; set; }

        [MaxLength(30)]
        public string? WorkPhone { get; set; }

        [MaxLength(10)]
        public string? WorkPhoneExtension { get; set; }

        [MaxLength(20)]
        public string? MaritalStatus { get; set; }

        [MaxLength(100)]
        public string? ResidencyType { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public int? EstimatedCreditScore { get; set; }

        public bool EConsentAuthorized { get; set; } = false;

        public bool CreditPullAuthorized { get; set; } = false;

        public int? NumberOfDependents { get; set; }

        [MaxLength(500)]
        public string? DependentAges { get; set; }

        public bool? MailingAddressSameAsCurrent { get; set; }

        [MaxLength(200)]
        public string? OtherLanguageDescription { get; set; }

        [MaxLength(2000)]
        public string? LanguagePreferences { get; set; }

        public int? ApplicationNumber { get; set; }

        public int? ApplicationBorrowerOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}