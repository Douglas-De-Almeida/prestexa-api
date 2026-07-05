using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class LoginRequest
    {
        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
        public string? DeviceId { get; set; }
    }
}