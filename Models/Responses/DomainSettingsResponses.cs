namespace PrestexaAPI.Models.Responses
{
    public class DomainSettingsResponse
    {
        public string Subdomain { get; set; } = string.Empty;

        public string FullUrl { get; set; } = string.Empty;

        public bool IsVerified { get; set; }
    }
}
