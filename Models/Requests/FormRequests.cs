using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class CreateFormDefinitionRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? FormType { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(50)]
        public string? Version { get; set; }

        public int? OperationalAssetId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateFormDefinitionRequest : CreateFormDefinitionRequest
    {
    }

    public class FormSetItemRequest
    {
        [Required]
        public int FormDefinitionId { get; set; }

        public int DisplayOrder { get; set; }
    }

    public class CreateFormSetRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public IReadOnlyList<FormSetItemRequest> Items { get; set; } = [];
    }

    public class UpdateFormSetRequest : CreateFormSetRequest
    {
    }
}