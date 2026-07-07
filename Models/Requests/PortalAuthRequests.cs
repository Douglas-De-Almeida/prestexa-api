using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class PortalRegisterRequest
    {
        [Required]
        public string NmlsNumber { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
    }

    public class PortalLoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
    }

    public class PortalVerifyOtpRequest
    {
        [Required]
        public string OtpToken { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
    }

    public class PortalForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}