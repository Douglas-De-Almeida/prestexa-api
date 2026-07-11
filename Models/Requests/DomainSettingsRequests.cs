using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class UpdateDomainSettingsRequest
    {
        [Required]
        [MaxLength(100)]
        public string Subdomain { get; set; } = string.Empty;
    }
}
