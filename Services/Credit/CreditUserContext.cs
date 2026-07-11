namespace PrestexaAPI.Services.Credit
{
    public class CreditUserContext
    {
        public int UserId { get; init; }
        public string? CompanyNmlsNumber { get; init; }
        public string Role { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;

        public bool IsSuperAdmin => string.Equals(Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        public bool IsLoanOfficer => string.Equals(Role, "LoanOfficer", StringComparison.OrdinalIgnoreCase);
    }
}
