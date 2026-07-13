namespace PrestexaAPI.Models
{
    public class ClientNeedsRule
    {
        public int Id { get; set; }
        public string CompanyNmlsNumber { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? TriggerEvent { get; set; }
        public string? ConditionJson { get; set; }
        public string? RequestedDocumentsJson { get; set; }
        public string? TargetRecipientType { get; set; }
        public string? Milestone { get; set; }
        public bool ReminderEnabled { get; set; }
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public int? UpdatedByUserId { get; set; }
    }
}