namespace PrestexaAPI.Models
{
    public class CompanyBranding
    {
        public int Id { get; set; }

        public string CompanyNmlsNumber { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public int? LightLogoAssetId { get; set; }

        public MediaAsset? LightLogoAsset { get; set; }

        public int? DarkLogoAssetId { get; set; }

        public MediaAsset? DarkLogoAsset { get; set; }

        public int? BackgroundAssetId { get; set; }

        public MediaAsset? BackgroundAsset { get; set; }

        public string PrimaryColor { get; set; } = "#1d4ce9";

        public string SecondaryColor { get; set; } = "#ffffff";

        public string AccentColor { get; set; } = "#2563eb";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}