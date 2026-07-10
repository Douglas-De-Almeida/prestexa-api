using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using PrestexaAPI.Models;

namespace PrestexaAPI.Models.Requests
{
    public class CreateLoanActivityRequest
    {
        public LoanActivityType Type { get; set; } = LoanActivityType.Note;

        [MaxLength(4000)]
        public string? Message { get; set; }

        public bool NotifyLoanTeam { get; set; }

        public JsonElement? Metadata { get; set; }
    }

    public class CreateLoanActivityReplyRequest
    {
        [Required]
        [MaxLength(4000)]
        public string Message { get; set; } = null!;

        public bool NotifyLoanTeam { get; set; }
    }
}
