using System.ComponentModel.DataAnnotations;

namespace PrestexaAPI.Models.Requests
{
    public class ManualMortgageApplicationRequest
    {
        [Required]
        public ManualLoanTermsRequest Loan { get; set; } = new();

        [Required]
        public ManualSubjectPropertyRequest SubjectProperty { get; set; } = new();

        [Required]
        [MinLength(1)]
        public List<ManualBorrowerRequest> Borrowers { get; set; } = new();

        public List<ManualHousingExpenseRequest> HousingExpenses { get; set; } = new();
    }

    public class ManualLoanTermsRequest
    {
        [Required]
        [MaxLength(50)]
        public string LoanPurposeType { get; set; } = "Purchase";

        [Required]
        [MaxLength(50)]
        public string MortgageType { get; set; } = "Conventional";

        [Required]
        [MaxLength(50)]
        public string LienPriorityType { get; set; } = "FirstLien";

        [Range(1, 100000000)]
        public decimal NoteAmount { get; set; }

        [Range(0, 100)]
        public decimal NoteRatePercent { get; set; }

        [Range(1, 480)]
        public int LoanAmortizationPeriodCount { get; set; } = 360;

        [Required]
        [MaxLength(50)]
        public string LoanAmortizationType { get; set; } = "Fixed";
    }

    public class ManualSubjectPropertyRequest
    {
        [Required]
        [MaxLength(200)]
        public string AddressLineText { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CityName { get; set; } = string.Empty;

        [Required]
        [MaxLength(2)]
        public string StateCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PropertyUsageType { get; set; } = "PrimaryResidence";

        [Range(1, 8)]
        public int FinancedUnitCount { get; set; } = 1;

        [Range(1, 100000000)]
        public decimal PropertyEstimatedValueAmount { get; set; }
    }

    public class ManualBorrowerRequest
    {
        [Required]
        [MaxLength(50)]
        public string BorrowerType { get; set; } = "PrimaryBorrower";

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? CellPhone { get; set; }

        [MaxLength(30)]
        public string? HomePhone { get; set; }

        [MaxLength(30)]
        public string? WorkPhone { get; set; }

        [MaxLength(20)]
        public string? MaritalStatus { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public int? EstimatedCreditScore { get; set; }

        public bool EConsentAuthorized { get; set; }

        public bool CreditPullAuthorized { get; set; }

        public List<ManualBorrowerAddressRequest> Addresses { get; set; } = new();

        public List<ManualBorrowerEmploymentRequest> Employments { get; set; } = new();

        public List<ManualBorrowerIncomeRequest> Incomes { get; set; } = new();

        public List<ManualBorrowerAssetRequest> Assets { get; set; } = new();

        public List<ManualBorrowerLiabilityRequest> Liabilities { get; set; } = new();

        public List<ManualBorrowerDeclarationRequest> Declarations { get; set; } = new();

        public ManualGovernmentMonitoringRequest? GovernmentMonitoring { get; set; }
    }

    public class ManualBorrowerAddressRequest
    {
        [Required]
        [MaxLength(50)]
        public string AddressType { get; set; } = "Current";

        [Required]
        [MaxLength(200)]
        public string StreetAddress { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(2)]
        public string? State { get; set; }

        [MaxLength(10)]
        public string? ZipCode { get; set; }

        [MaxLength(100)]
        public string? OccupancyType { get; set; }

        public int? YearsSpent { get; set; }

        public int? MonthsSpent { get; set; }
    }

    public class ManualBorrowerEmploymentRequest
    {
        [Required]
        [MaxLength(200)]
        public string EmployerName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string EmploymentStatusType { get; set; } = "Current";

        public DateTime? EmploymentStartDate { get; set; }

        [MaxLength(100)]
        public string? EmploymentPositionDescription { get; set; }
    }

    public class ManualBorrowerIncomeRequest
    {
        [Required]
        [MaxLength(50)]
        public string IncomeType { get; set; } = string.Empty;

        [Range(0, 100000000)]
        public decimal CurrentIncomeMonthlyTotalAmount { get; set; }
    }

    public class ManualBorrowerAssetRequest
    {
        [Required]
        [MaxLength(50)]
        public string AssetType { get; set; } = string.Empty;

        [Range(0, 100000000)]
        public decimal AssetCashOrMarketValueAmount { get; set; }
    }

    public class ManualBorrowerLiabilityRequest
    {
        [Required]
        [MaxLength(50)]
        public string LiabilityType { get; set; } = string.Empty;

        [Range(0, 100000000)]
        public decimal MonthlyPaymentAmount { get; set; }

        [Range(0, 100000000)]
        public decimal UPBAmount { get; set; }
    }

    public class ManualBorrowerDeclarationRequest
    {
        [Required]
        [MaxLength(100)]
        public string DeclarationType { get; set; } = string.Empty;

        public bool IsAffirmative { get; set; }

        [MaxLength(1000)]
        public string? Explanation { get; set; }
    }

    public class ManualGovernmentMonitoringRequest
    {
        [MaxLength(100)]
        public string? EthnicityType { get; set; }

        [MaxLength(100)]
        public string? RaceType { get; set; }

        [MaxLength(100)]
        public string? SexType { get; set; }

        [MaxLength(100)]
        public string? CollectionMethodType { get; set; }
    }

    public class ManualHousingExpenseRequest
    {
        [Required]
        [MaxLength(50)]
        public string HousingExpenseType { get; set; } = string.Empty;

        [Range(0, 100000000)]
        public decimal PresentHousingExpenseAmount { get; set; }

        [Range(0, 100000000)]
        public decimal ProposedHousingExpenseAmount { get; set; }
    }
}
