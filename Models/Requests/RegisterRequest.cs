using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class RegisterRequest
    {
        [Required]
        public string NmlsNumber { get; set; } = null!;

        [Required]
        public string FirstName { get; set; } = null!;

        public string? MiddleName { get; set; }

        [Required]
        public string LastName { get; set; } = null!;

        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Required]
        public string PhoneNumber { get; set; } = null!;
    }
}