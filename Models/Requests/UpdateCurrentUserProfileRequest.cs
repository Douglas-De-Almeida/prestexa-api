using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class UpdateCurrentUserProfileRequest
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [MaxLength(30)]
        public string? OfficePhone { get; set; }

        [MaxLength(10)]
        public string? OfficePhoneExtension { get; set; }

        [MaxLength(30)]
        public string? MobilePhone { get; set; }

        [MaxLength(100)]
        public string? ClientFacingTitle { get; set; }

        [MaxLength(1000)]
        public string? FacebookProfileUrl { get; set; }

        [MaxLength(100)]
        public string? TwitterHandle { get; set; }

        [MaxLength(1000)]
        public string? LinkedInProfileUrl { get; set; }

        [MaxLength(100)]
        public string? InstagramHandle { get; set; }

        [MaxLength(1000)]
        public string? ProfilePhotoUrl { get; set; }

        public int? ProfilePhotoAssetId { get; set; }
    }
}
