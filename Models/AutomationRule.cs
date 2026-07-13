namespace PrestexaAPI.Models
{
    public class AutomationRule
    {
        public int Id { get; set; }
        public string CompanyNmlsNumber { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? TriggerType { get; set; }
        public string? ActionType { get; set; }
        public string? TriggerJson { get; set; }
        public string? ActionJson { get; set; }
        public string? Milestone { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public int? UpdatedByUserId { get; set; }
    }
}