using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string CompanyNmlsNumber { get; set; } = null!;

        public Company Company { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "User";

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [MaxLength(30)]
        public string PhoneNumber { get; set; } = null!;

        [MaxLength(20)]
        public string? UserNmlsNumber { get; set; }

        [MaxLength(30)]
        public string? OfficePhone { get; set; }

        [MaxLength(10)]
        public string? OfficePhoneExtension { get; set; }

        [MaxLength(30)]
        public string? MobilePhone { get; set; }

        [MaxLength(100)]
        public string? ClientFacingTitle { get; set; }

        [MaxLength(1000)]
        public string? ProfilePhotoPath { get; set; }

        public SeatType SeatType { get; set; } = SeatType.Originator;

        public UserStatus Status { get; set; } = UserStatus.Active;

        public DateTime? StartDate { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public bool TwoFactorEnabled { get; set; } = false;

public string? TwoFactorSecret { get; set; }

public DateTime? TwoFactorEnabledAt { get; set; }

public DateTime? TwoFactorLastVerifiedAt { get; set; }
    }
}