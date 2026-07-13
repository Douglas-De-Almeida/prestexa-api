namespace PrestexaAPI.Models.Responses
{
    public class FormDefinitionResponse
    {
        public int Id { get; set; }
        public string CompanyNmlsNumber { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? FormType { get; set; }
        public string? Category { get; set; }
        public string? Version { get; set; }
        public int? OperationalAssetId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }

    public class FormDefinitionListResponse
    {
        public IReadOnlyList<FormDefinitionResponse> Items { get; set; } = [];
    }

    public class FormSetItemResponse
    {
        public int Id { get; set; }
        public int FormSetId { get; set; }
        public int FormDefinitionId { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class FormSetResponse
    {
        public int Id { get; set; }
        public string CompanyNmlsNumber { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public IReadOnlyList<FormSetItemResponse> Items { get; set; } = [];
    }

    public class FormSetListResponse
    {
        public IReadOnlyList<FormSetResponse> Items { get; set; } = [];
    }

    public class ClientNeedsRuleResponse
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
        public bool IsEnabled { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }

    public class ClientNeedsRuleListResponse
    {
        public IReadOnlyList<ClientNeedsRuleResponse> Items { get; set; } = [];
    }

    public class AutomationRuleResponse
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
        public bool IsEnabled { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }

    public class AutomationRuleListResponse
    {
        public IReadOnlyList<AutomationRuleResponse> Items { get; set; } = [];
    }

    public class ClosingCostItemResponse
    {
        public int Id { get; set; }
        public string CompanyNmlsNumber { get; set; } = null!;
        public string FeeName { get; set; } = null!;
        public string? FeeCategory { get; set; }
        public decimal Amount { get; set; }
        public decimal? Percentage { get; set; }
        public string? PaidBy { get; set; }
        public bool IsFinanceCharge { get; set; }
        public bool IsAprFee { get; set; }
        public string? StateApplicability { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }

    public class ClosingCostItemListResponse
    {
        public IReadOnlyList<ClosingCostItemResponse> Items { get; set; } = [];
    }
}