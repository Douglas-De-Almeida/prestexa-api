using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class LoanActivity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        public int LoanId { get; set; }

        public Loan Loan { get; set; } = null!;

        [Required]
        [StringLength(8)]
        public string LoanNumber { get; set; } = null!;

        public int? ParentActivityId { get; set; }

        public LoanActivity? ParentActivity { get; set; }

        public LoanActivityType ActivityType { get; set; } = LoanActivityType.Note;

        [MaxLength(4000)]
        public string? Message { get; set; }

        public string? MetadataJson { get; set; }

        public bool NotifyLoanTeam { get; set; }

        public LoanActivityVisibility Visibility { get; set; } = LoanActivityVisibility.InternalOnly;

        public int? ActorUserId { get; set; }

        [MaxLength(255)]
        public string? ActorName { get; set; }

        [MaxLength(100)]
        public string? ActorRole { get; set; }

        public LoanActivityActorType ActorType { get; set; } = LoanActivityActorType.User;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
