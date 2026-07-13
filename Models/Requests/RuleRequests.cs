using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class CreateClientNeedsRuleRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? TriggerEvent { get; set; }

        public string? ConditionJson { get; set; }

        public string? RequestedDocumentsJson { get; set; }

        [MaxLength(100)]
        public string? TargetRecipientType { get; set; }

        [MaxLength(100)]
        public string? Milestone { get; set; }

        public bool ReminderEnabled { get; set; }

        public bool IsEnabled { get; set; } = true;

        public int Priority { get; set; }
    }

    public class UpdateClientNeedsRuleRequest : CreateClientNeedsRuleRequest
    {
    }

    public class CreateAutomationRuleRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? TriggerType { get; set; }

        [MaxLength(100)]
        public string? ActionType { get; set; }

        public string? TriggerJson { get; set; }

        public string? ActionJson { get; set; }

        [MaxLength(100)]
        public string? Milestone { get; set; }

        public bool IsEnabled { get; set; } = true;

        public int Priority { get; set; }
    }

    public class UpdateAutomationRuleRequest : CreateAutomationRuleRequest
    {
    }
}