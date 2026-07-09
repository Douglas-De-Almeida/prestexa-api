namespace PrestexaAPI.Services.Mismo
{
    public class ParsedMismoLoanData
    {
        public string? MismoVersion { get; set; }

        public ParsedMismoLoanTerms LoanTerms { get; set; } = new();

        public ParsedMismoSubjectProperty SubjectProperty { get; set; } = new();

        public List<ParsedMismoBorrower> Borrowers { get; set; } = new();

        public List<ParsedMismoAsset> Assets { get; set; } = new();

        public List<ParsedMismoLiability> Liabilities { get; set; } = new();
    }

    public class ParsedMismoLoanTerms
    {
        public string LoanPurposeType { get; set; } = "Purchase";
        public string LienPriorityType { get; set; } = "FirstLien";
        public string MortgageType { get; set; } = "Conventional";
        public decimal NoteAmount { get; set; }
        public decimal NoteRatePercent { get; set; }
        public int LoanAmortizationPeriodCount { get; set; } = 360;
        public string LoanAmortizationType { get; set; } = "Fixed";
    }

    public class ParsedMismoSubjectProperty
    {
        public string AddressLineText { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string PropertyUsageType { get; set; } = "PrimaryResidence";
        public int FinancedUnitCount { get; set; } = 1;
        public decimal PropertyEstimatedValueAmount { get; set; }
    }

    public class ParsedMismoBorrower
    {
        public string BorrowerRoleType { get; set; } = "Borrower";
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? EmailAddressText { get; set; }
        public ParsedMismoBorrowerAddress? CurrentAddress { get; set; }
        public List<ParsedMismoEmployment> Employments { get; set; } = new();
        public List<ParsedMismoIncome> Incomes { get; set; } = new();
    }

    public class ParsedMismoBorrowerAddress
    {
        public string AddressLineText { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string BorrowerResidencyType { get; set; } = "Current";
        public int? ResidencyDurationMonthsCount { get; set; }
    }

    public class ParsedMismoEmployment
    {
        public string EmployerName { get; set; } = string.Empty;
        public string EmploymentStatusType { get; set; } = "Current";
        public DateTime? EmploymentStartDate { get; set; }
        public string? EmploymentPositionDescription { get; set; }
    }

    public class ParsedMismoIncome
    {
        public string IncomeType { get; set; } = string.Empty;
        public decimal CurrentIncomeMonthlyTotalAmount { get; set; }
    }

    public class ParsedMismoAsset
    {
        public string AssetType { get; set; } = string.Empty;
        public decimal AssetCashOrMarketValueAmount { get; set; }
    }

    public class ParsedMismoLiability
    {
        public string LiabilityType { get; set; } = string.Empty;
        public decimal MonthlyPaymentAmount { get; set; }
        public decimal UPBAmount { get; set; }
    }

    public class MismoImportResult
    {
        public int LoanId { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public string CompanyNmlsNumber { get; set; } = string.Empty;
        public string? MismoVersion { get; set; }
        public string ContentSha256 { get; set; } = string.Empty;
        public bool IsDuplicate { get; set; }
        public int? ExistingLoanId { get; set; }
        public string? ExistingLoanNumber { get; set; }
        public int BorrowerCount { get; set; }
        public int AssetCount { get; set; }
        public int LiabilityCount { get; set; }
        public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
