using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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

        public ManualLoanStructureRequest LoanStructure { get; set; } = new();

        public ManualTitleInfoRequest TitleInfo { get; set; } = new();

        public List<ManualDownPaymentSourceRequest> DownPaymentSources { get; set; } = new();

        public List<ManualPurchaseCreditRequest> PurchaseCredits { get; set; } = new();

        public List<ManualSubordinateLienRequest> SubordinateLiens { get; set; } = new();

        public ManualHousingExpenseInputsRequest? HousingExpenseInputs { get; set; }

        public List<ManualBorrowerApplicationRequest> BorrowerApplications { get; set; } = new();
    }

    public class ManualBorrowerApplicationRequest
    {
        public int ApplicationNumber { get; set; }

        public List<JsonElement> BorrowerIds { get; set; } = new();
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

        [Range(0, 100000000)]
        public decimal? BaseLoanAmount { get; set; }

        [Range(0, 100000000)]
        public decimal? TotalLoanAmount { get; set; }

        [Range(0, 100)]
        public decimal NoteRatePercent { get; set; }

        [Range(0, 100)]
        public decimal? QualifyingRate { get; set; }

        [Range(1, 480)]
        public int LoanAmortizationPeriodCount { get; set; } = 360;

        [Required]
        [MaxLength(50)]
        public string LoanAmortizationType { get; set; } = "Fixed";

        [MaxLength(50)]
        public string? InterestRateBuydown { get; set; }

        public bool? InterestOnly { get; set; }

        [Range(0, 480)]
        public int? InterestOnlyTermMonths { get; set; }

        [MaxLength(50)]
        public string? ImpoundWaiver { get; set; }

        public int? LoanFico { get; set; }

        public bool? NoFico { get; set; }

        [Range(0, 120)]
        public int? ProjectedReserveMonths { get; set; }

        [Range(0, 1000)]
        public decimal? Ltv { get; set; }

        [Range(0, 1000)]
        public decimal? Cltv { get; set; }

        [Range(0, 1000)]
        public decimal? Hcltv { get; set; }

        [MaxLength(100)]
        public string? RefinanceType { get; set; }

        [MaxLength(100)]
        public string? RefinanceProgram { get; set; }

        [Range(0, 100000000)]
        public decimal? ExistingLiensAmount { get; set; }
    }

    public class ManualSubjectPropertyRequest
    {
        [Required]
        [MaxLength(200)]
        public string AddressLineText { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? UnitIdentifier { get; set; }

        [Required]
        [MaxLength(100)]
        public string CityName { get; set; } = string.Empty;

        [Required]
        [MaxLength(2)]
        public string StateCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string PostalCode { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? County { get; set; }

        [Required]
        [MaxLength(50)]
        public string PropertyUsageType { get; set; } = "PrimaryResidence";

        [MaxLength(100)]
        public string? PropertyType { get; set; }

        [MaxLength(100)]
        public string? ConstructionMethod { get; set; }

        [MaxLength(100)]
        public string? Occupancy { get; set; }

        [Range(1, 8)]
        public int FinancedUnitCount { get; set; } = 1;

        public bool? MixedUseProperty { get; set; }

        public bool? NonOccupantCoBorrower { get; set; }

        public bool? CommunityPropertyState { get; set; }

        public bool? EnergyImprovement { get; set; }

        public bool? SolarLienPriority { get; set; }

        public bool? ConversionOfContract { get; set; }

        public bool? Renovation { get; set; }

        public bool? ConstructionLoan { get; set; }

        public DateTime? LotAcquiredDate { get; set; }

        [Range(0, 100000000)]
        public decimal? OriginalCostOfLot { get; set; }

        [MaxLength(100)]
        public string? AttachmentType { get; set; }

        [MaxLength(100)]
        public string? StructureType { get; set; }

        [MaxLength(100)]
        public string? DesignType { get; set; }

        public int? YearBuilt { get; set; }

        [Range(0, 1000000)]
        public decimal? Acreage { get; set; }

        [MaxLength(100)]
        public string? Improvements { get; set; }

        [Range(0, 100000000)]
        public decimal? ImprovementCosts { get; set; }

        public int? AccessoryDwellingUnitCount { get; set; }

        [MaxLength(100)]
        public string? ConstructionStatus { get; set; }

        [MaxLength(200)]
        public string? ProjectName { get; set; }

        [MaxLength(100)]
        public string? CondoProjectManagerId { get; set; }

        [MaxLength(2500)]
        public string? LegalDescription { get; set; }

        [Range(1, 100000000)]
        public decimal? PropertyEstimatedValueAmount { get; set; }
    }

    public class ManualBorrowerRequest
    {
        public int? Id { get; set; }

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

        [MaxLength(20)]
        public string? Suffix { get; set; }

        [MaxLength(100)]
        public string? Nickname { get; set; }

        [MaxLength(20)]
        public string? SsnItin { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? CellPhone { get; set; }

        [MaxLength(30)]
        public string? HomePhone { get; set; }

        [MaxLength(30)]
        public string? WorkPhone { get; set; }

        [MaxLength(10)]
        public string? WorkPhoneExtension { get; set; }

        [JsonPropertyName("workPhoneExt")]
        public string? WorkPhoneExt
        {
            get => WorkPhoneExtension;
            set => WorkPhoneExtension = value;
        }

        [MaxLength(20)]
        public string? MaritalStatus { get; set; }

        public JsonElement? DateOfBirth { get; set; }

        [JsonConverter(typeof(NullableIntStringJsonConverter))]
        public int? EstimatedCreditScore { get; set; }

        public bool EConsentAuthorized { get; set; }

        public bool CreditPullAuthorized { get; set; }

        public int? NumberOfDependents { get; set; }

        [MaxLength(500)]
        public string? DependentAges { get; set; }

        private bool? _mailingAddressSameAsCurrent;

        public bool? MailingAddressSameAsCurrent
        {
            get => _mailingAddressSameAsCurrent;
            set => _mailingAddressSameAsCurrent = value;
        }

        [JsonPropertyName("mailingAddressIsSameAsCurrentAddress")]
        public bool? MailingAddressIsSameAsCurrentAddress
        {
            get => MailingAddressSameAsCurrent;
            set => MailingAddressSameAsCurrent = value;
        }

        [JsonPropertyName("mailingAddressNotSameAsCurrentAddress")]
        public bool? MailingAddressNotSameAsCurrentAddress
        {
            get => MailingAddressSameAsCurrent.HasValue ? !MailingAddressSameAsCurrent.Value : null;
            set => MailingAddressSameAsCurrent = value.HasValue ? !value.Value : null;
        }

        public JsonElement? LanguagePreferences { get; set; }

        [MaxLength(200)]
        public string? OtherLanguageDescription { get; set; }

        [JsonPropertyName("languageOtherDescription")]
        public string? LanguageOtherDescription
        {
            get => OtherLanguageDescription;
            set => OtherLanguageDescription = value;
        }

        public int? ApplicationNumber { get; set; }

        public int? ApplicationBorrowerOrder { get; set; }

        [JsonPropertyName("borrowerOrder")]
        public int? BorrowerOrder
        {
            get => ApplicationBorrowerOrder;
            set => ApplicationBorrowerOrder = value;
        }

        public List<ManualBorrowerAddressRequest> Addresses { get; set; } = new();

        public List<ManualBorrowerEmploymentRequest> Employments { get; set; } = new();

        public List<ManualBorrowerIncomeRequest> Incomes { get; set; } = new();

        public List<ManualBorrowerAssetRequest> Assets { get; set; } = new();

        public List<ManualBorrowerLiabilityRequest> Liabilities { get; set; } = new();

        public List<ManualBorrowerDeclarationRequest> Declarations { get; set; } = new();

        public ManualGovernmentMonitoringRequest? GovernmentMonitoring { get; set; }

        public ManualBorrowerMilitaryInfoRequest? MilitaryInformation { get; set; }
    }

    public class ManualBorrowerAddressRequest
    {
        [Required]
        [MaxLength(50)]
        public string AddressType { get; set; } = "Current";

        [MaxLength(200)]
        public string? StreetAddress { get; set; }

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
        [MaxLength(200)]
        public string? EmployerName { get; set; }

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

        [JsonConverter(typeof(NullableDecimalStringJsonConverter))]
        [Range(0, 100000000)]
        public decimal? MonthlyPaymentAmount { get; set; }

        [JsonConverter(typeof(NullableDecimalStringJsonConverter))]
        [Range(0, 100000000)]
        public decimal? UPBAmount { get; set; }
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
        public bool? ToBeCompletedByFinancialInstitution { get; set; }

        [MaxLength(100)]
        public string? EthnicityType { get; set; }

        [MaxLength(200)]
        public string? EthnicityOriginOther { get; set; }

        [MaxLength(100)]
        public string? RaceType { get; set; }

        [MaxLength(200)]
        public string? RaceOther { get; set; }

        [MaxLength(100)]
        public string? SexType { get; set; }

        [MaxLength(100)]
        public string? CollectionMethodType { get; set; }

        public bool? CollectedByVisualObservation { get; set; }

        public bool? CollectedEthnicityByObservation { get; set; }

        public bool? CollectedRaceByObservation { get; set; }

        public bool? CollectedSexByObservation { get; set; }

        [MaxLength(100)]
        public string? HmdaMonitoringType { get; set; }

        public bool? EthnicityVisualObservationIndicator { get; set; }

        public bool? RaceVisualObservationIndicator { get; set; }

        public bool? SexVisualObservationIndicator { get; set; }
    }

    public class ManualBorrowerMilitaryInfoRequest
    {
        [MaxLength(100)]
        public string? MilitaryStatus { get; set; }

        public bool? ActiveDuty { get; set; }

        public bool? Veteran { get; set; }

        public bool? Reserves { get; set; }

        public bool? NationalGuard { get; set; }

        [MaxLength(100)]
        public string? ServiceBranch { get; set; }

        public DateTime? ServiceStartDate { get; set; }

        public DateTime? ServiceEndDate { get; set; }

        public DateTime? ProjectedExpirationDate { get; set; }

        public bool? SurvivorSpouse { get; set; }

        public bool? VaEligible { get; set; }

        [MaxLength(100)]
        public string? VaEligibilityStatus { get; set; }

        public bool? VaFirstTimeUse { get; set; }

        public bool? VaFundingFeeExempt { get; set; }
    }

    public class ManualHousingExpenseRequest
    {
        [MaxLength(50)]
        public string HousingExpenseType { get; set; } = string.Empty;

        [JsonPropertyName("expenseType")]
        public string? ExpenseType
        {
            get => HousingExpenseType;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    HousingExpenseType = value;
            }
        }

        [MaxLength(50)]
        public string? Mode { get; set; }

        [JsonPropertyName("frequency")]
        public string? Frequency
        {
            get => Mode;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    Mode = value;
            }
        }

        [Range(0, 100000000)]
        public decimal? Factor { get; set; }

        [Range(0, 100000000)]
        public decimal? ValueAmount { get; set; }

        [MaxLength(100)]
        public string? SourceType { get; set; }

        [MaxLength(200)]
        public string? Description { get; set; }

        [Range(0, 100)]
        public decimal? InitialAnnualRate { get; set; }

        [Range(0, 100000000)]
        public decimal? InitialMonthlyAmount { get; set; }

        [Range(0, 100)]
        public decimal? RenewalAnnualRate { get; set; }

        [Range(0, 100000000)]
        public decimal? RenewalMonthlyAmount { get; set; }

        public bool? Included { get; set; }

        [JsonPropertyName("includeInPiti")]
        public bool? IncludeInPiti
        {
            get => Included;
            set => Included = value;
        }

        public int? DisplayOrder { get; set; }

        [Range(0, 100000000)]
        public decimal PresentHousingExpenseAmount { get; set; }

        [Range(0, 100000000)]
        public decimal ProposedHousingExpenseAmount { get; set; }

        [JsonPropertyName("amount")]
        [Range(0, 100000000)]
        public decimal? Amount { get; set; }
    }

    public class ManualLoanStructureRequest
    {
        [Range(0, 100000000)]
        public decimal? PurchasePrice { get; set; }

        [Range(0, 100000000)]
        public decimal? DownPaymentAmount { get; set; }

        [Range(0, 1000)]
        public decimal? DownPaymentPercent { get; set; }

        [Range(0, 100000000)]
        public decimal? BaseLoanAmount { get; set; }

        [Range(0, 100000000)]
        public decimal? TotalLoanAmount { get; set; }

        [Range(0, 100000000)]
        public decimal? SubordinateLienAmount { get; set; }

        [Range(0, 100)]
        public decimal? QualifyingRate { get; set; }

        public bool? InterestOnly { get; set; }

        [Range(0, 480)]
        public int? InterestOnlyTermMonths { get; set; }

        [MaxLength(50)]
        public string? InterestRateBuydown { get; set; }

        [MaxLength(50)]
        public string? ImpoundWaiver { get; set; }

        public int? LoanFico { get; set; }

        public bool? NoFico { get; set; }

        [Range(0, 120)]
        public int? ProjectedReserveMonths { get; set; }

        [Range(0, 1000)]
        public decimal? Ltv { get; set; }

        [Range(0, 1000)]
        public decimal? Cltv { get; set; }

        [Range(0, 1000)]
        public decimal? Hcltv { get; set; }

        [MaxLength(100)]
        public string? RefinanceType { get; set; }

        [MaxLength(100)]
        public string? RefinanceProgram { get; set; }

        [Range(0, 100000000)]
        public decimal? ExistingLiensAmount { get; set; }

        public bool? LpaOffering { get; set; }

        public bool? AlternateDoc { get; set; }

        public bool? TemporaryBuydown { get; set; }

        public bool? BusinessPurpose { get; set; }

        public bool? BridgeLoan { get; set; }

        public bool? AssetDepletion { get; set; }

        [MaxLength(50)]
        public string? InvestmentIncomeMode { get; set; }

        [Range(0, 100000000)]
        public decimal? MonthlyGrossRentalIncome { get; set; }

        [Range(0, 1000)]
        public decimal? RentalPercentage { get; set; }

        [Range(0, 100000000)]
        public decimal? TotalCalculatedRent { get; set; }

        [Range(0, 100000000)]
        public decimal? TotalExpenses { get; set; }

        [Range(-100000000, 100000000)]
        public decimal? NetSubjectIncome { get; set; }

        [Range(0, 100000000)]
        public decimal? FirstMortgage { get; set; }

        [Range(0, 100000000)]
        public decimal? OtherFinancing { get; set; }

        [MaxLength(50)]
        public string? OtherFinancingMode { get; set; }

        [Range(0, 100000000)]
        public decimal? OtherFinancingFactor { get; set; }

        [Range(0, 100000000)]
        public decimal? OtherFinancingValue { get; set; }

        [Range(0, 100000000)]
        public decimal? Hoi { get; set; }

        [MaxLength(50)]
        public string? HoiMode { get; set; }

        [Range(0, 100000000)]
        public decimal? HoiFactor { get; set; }

        [MaxLength(100)]
        public string? HoiSource { get; set; }

        [Range(0, 100000000)]
        public decimal? Supplemental { get; set; }

        [MaxLength(50)]
        public string? SupplementalMode { get; set; }

        [Range(0, 100000000)]
        public decimal? SupplementalFactor { get; set; }

        [MaxLength(100)]
        public string? SupplementalSource { get; set; }

        [Range(0, 100000000)]
        public decimal? PropertyTaxes { get; set; }

        [MaxLength(50)]
        public string? PropertyTaxesMode { get; set; }

        [Range(0, 100000000)]
        public decimal? PropertyTaxesFactor { get; set; }

        [MaxLength(100)]
        public string? PropertyTaxesSource { get; set; }

        [MaxLength(50)]
        public string? MiMode { get; set; }

        [Range(0, 100)]
        public decimal? MiInitialAnnualRate { get; set; }

        [Range(0, 100000000)]
        public decimal? MiInitialMonthly { get; set; }

        [Range(0, 100)]
        public decimal? MiRenewalAnnualRate { get; set; }

        [Range(0, 100000000)]
        public decimal? MiRenewalMonthly { get; set; }

        [Range(0, 100000000)]
        public decimal? AssociationDues { get; set; }

        [MaxLength(200)]
        public string? OtherDescription { get; set; }

        [Range(0, 100000000)]
        public decimal? OtherAmount { get; set; }

        [Range(0, 100000000)]
        public decimal? TotalPiti { get; set; }
    }

    public class ManualTitleInfoRequest
    {
        [MaxLength(100)]
        public string? MannerTitleHeld { get; set; }

        [MaxLength(500)]
        public string? TitleHeldInNameOf { get; set; }

        [MaxLength(100)]
        public string? VestingToRead { get; set; }

        [MaxLength(100)]
        public string? TrustInformation { get; set; }

        [MaxLength(100)]
        public string? IndianCountryLandTenure { get; set; }

        [MaxLength(100)]
        public string? EstateHeldIn { get; set; }

        [MaxLength(200)]
        public string? TitleInsuranceCompany { get; set; }

        [MaxLength(100)]
        public string? TitleCommitmentNumber { get; set; }

        [MaxLength(100)]
        public string? TitlePolicyNumber { get; set; }

        [MaxLength(100)]
        public string? VestingEntityType { get; set; }

        [MaxLength(2000)]
        public string? TitleNotes { get; set; }

        public bool? Homestead { get; set; }

        [JsonPropertyName("homesteadExemption")]
        public bool? HomesteadExemption
        {
            get => Homestead;
            set => Homestead = value;
        }

        public bool? TitleCurative { get; set; }

        [JsonPropertyName("titleCurativeRequired")]
        public bool? TitleCurativeRequired
        {
            get => TitleCurative;
            set => TitleCurative = value;
        }

        public bool? PowerOfAttorney { get; set; }

        public bool? RecordedEasement { get; set; }
    }

    public class ManualDownPaymentSourceRequest
    {
        public int? Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string SourceType { get; set; } = string.Empty;

        [Range(0, 100000000)]
        [JsonConverter(typeof(NullableDecimalStringJsonConverter))]
        public decimal? Amount { get; set; }

        public bool Included { get; set; } = true;

        public int DisplayOrder { get; set; }
    }

    public class ManualPurchaseCreditRequest
    {
        public int? Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string CreditType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string SourceType { get; set; } = string.Empty;

        [Range(0, 100000000)]
        [JsonConverter(typeof(NullableDecimalStringJsonConverter))]
        public decimal? Amount { get; set; }

        public int DisplayOrder { get; set; }
    }

    public class ManualSubordinateLienRequest
    {
        public int? Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string LienType { get; set; } = string.Empty;

        [JsonPropertyName("lienPosition")]
        public string? LienPosition
        {
            get => LienType;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    LienType = value;
            }
        }

        [MaxLength(200)]
        public string? LienName { get; set; }

        [JsonPropertyName("creditorName")]
        public string? CreditorName
        {
            get => LienName;
            set => LienName = value;
        }

        [JsonConverter(typeof(NullableDecimalStringJsonConverter))]
        [Range(0, 100000000)]
        public decimal? MonthlyPaymentAmount { get; set; }

        [JsonPropertyName("monthlyPayment")]
        [JsonConverter(typeof(NullableDecimalStringJsonConverter))]
        [Range(0, 100000000)]
        public decimal? MonthlyPayment
        {
            get => MonthlyPaymentAmount;
            set => MonthlyPaymentAmount = value;
        }

        [JsonConverter(typeof(NullableDecimalStringJsonConverter))]
        [Range(0, 100000000)]
        public decimal? LoanAmount { get; set; }

        [MaxLength(100)]
        public string? SourceType { get; set; }

        [JsonPropertyName("fundSourceType")]
        public string? FundSourceType
        {
            get => SourceType;
            set => SourceType = value;
        }

        public int DisplayOrder { get; set; }
    }

    public class ManualHousingExpenseInputsRequest
    {
        public ManualCalcInputRequest? OtherFinancing { get; set; }

        public ManualSourceCalcInputRequest? Hoi { get; set; }

        public ManualSourceCalcInputRequest? Supplemental { get; set; }

        public ManualSourceCalcInputRequest? PropertyTaxes { get; set; }

        public ManualMiInputRequest? Mi { get; set; }

        public ManualValueInputRequest? AssociationDues { get; set; }

        public ManualOtherInputRequest? Other { get; set; }
    }

    public class ManualCalcInputRequest
    {
        public bool? IsCalc { get; set; }

        [MaxLength(50)]
        public string? Factor { get; set; }

        [MaxLength(100)]
        public string? Value { get; set; }
    }

    public class ManualSourceCalcInputRequest : ManualCalcInputRequest
    {
        [MaxLength(50)]
        public string? Percent { get; set; }

        [MaxLength(100)]
        public string? Source { get; set; }
    }

    public class ManualMiInputRequest
    {
        [MaxLength(50)]
        public string? Mode { get; set; }

        [MaxLength(50)]
        public string? Factor { get; set; }

        [MaxLength(100)]
        public string? Value { get; set; }

        [MaxLength(100)]
        public string? Source { get; set; }

        public ManualMiRatesInputRequest? MortgageInsurance { get; set; }
    }

    public class ManualMiRatesInputRequest
    {
        [MaxLength(50)]
        public string? InitialAnnualRate { get; set; }

        [MaxLength(100)]
        public string? InitialMonthly { get; set; }

        [MaxLength(50)]
        public string? RenewalAnnualRate { get; set; }

        [MaxLength(100)]
        public string? RenewalMonthly { get; set; }
    }

    public class ManualValueInputRequest
    {
        [MaxLength(100)]
        public string? Value { get; set; }
    }

    public class ManualOtherInputRequest : ManualValueInputRequest
    {
        [MaxLength(200)]
        public string? Description { get; set; }
    }

    internal sealed class NullableDecimalStringJsonConverter : JsonConverter<decimal?>
    {
        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetDecimal(out var numberValue))
                    return numberValue;

                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                if (string.IsNullOrWhiteSpace(value))
                    return null;

                var normalized = value.Trim().Replace("$", string.Empty).Replace(",", string.Empty);
                if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                    return parsed;

                return null;
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteNumberValue(value.Value);
                return;
            }

            writer.WriteNullValue();
        }
    }

    internal sealed class NullableIntStringJsonConverter : JsonConverter<int?>
    {
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out var numberValue))
                    return numberValue;

                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                return ParseCreditScoreValue(reader.GetString());
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var jsonDoc = JsonDocument.ParseValue(ref reader);
                var root = jsonDoc.RootElement;

                if (TryReadObjectProperty(root, "value", out var value))
                    return ParseCreditScoreElement(value);

                if (TryReadObjectProperty(root, "label", out var label))
                    return ParseCreditScoreElement(label);

                if (TryReadObjectProperty(root, "score", out var score))
                    return ParseCreditScoreElement(score);

                return null;
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteNumberValue(value.Value);
                return;
            }

            writer.WriteNullValue();
        }

        private static int? ParseCreditScoreElement(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numeric))
                return numeric;

            if (element.ValueKind == JsonValueKind.String)
                return ParseCreditScoreValue(element.GetString());

            return null;
        }

        private static int? ParseCreditScoreValue(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return null;

            var value = rawValue.Trim();

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                return parsed;

            var rangeMatch = Regex.Match(value, "(?<start>\\d{3})\\s*-\\s*(?<end>\\d{3})");
            if (rangeMatch.Success
                && int.TryParse(rangeMatch.Groups["start"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var start)
                && int.TryParse(rangeMatch.Groups["end"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var end))
            {
                return (start + end) / 2;
            }

            var plusMatch = Regex.Match(value, "(?<base>\\d{3})\\s*\\+");
            if (plusMatch.Success
                && int.TryParse(plusMatch.Groups["base"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var plusBase))
            {
                return plusBase;
            }

            var belowMatch = Regex.Match(value, "below\\s*(?<base>\\d{3})", RegexOptions.IgnoreCase);
            if (belowMatch.Success
                && int.TryParse(belowMatch.Groups["base"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var belowBase))
            {
                return belowBase - 1;
            }

            return null;
        }

        private static bool TryReadObjectProperty(JsonElement element, string propertyName, out JsonElement propertyValue)
        {
            propertyValue = default;

            foreach (var property in element.EnumerateObject())
            {
                if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                propertyValue = property.Value;
                return true;
            }

            return false;
        }
    }
}
