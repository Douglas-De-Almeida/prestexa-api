namespace PrestexaAPI.Models
{
    public class FormDefinition
    {
        public int Id { get; set; }
        public string CompanyNmlsNumber { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? FormType { get; set; }
        public string? Category { get; set; }
        public string? Version { get; set; }
        public int? OperationalAssetId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public int? UpdatedByUserId { get; set; }
    }
}