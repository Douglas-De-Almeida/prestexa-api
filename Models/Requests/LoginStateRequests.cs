using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class StartLoginRequest
    {
        [Required]
        [MaxLength(2000)]
        public string ReturnUrl { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Portal { get; set; } = "los";
    }

    public class ConsumeLoginStateRequest
    {
        [Required]
        public string State { get; set; } = string.Empty;
    }
}