using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrestexaAPI.Models.Responses
{
    public class ManualMortgageApplicationResponse
    {
        public string LoanNumber { get; set; } = string.Empty;

        public ManualLoanSnapshotResponse Loan { get; set; } = new();

        public ManualSubjectPropertySnapshotResponse? SubjectProperty { get; set; }

        public List<ManualHousingExpenseSnapshotResponse> HousingExpenses { get; set; } = new();

        public List<ManualBorrowerSnapshotResponse> Borrowers { get; set; } = new();

        public int? Fico { get; set; }

        public decimal? Ltv { get; set; }

        public decimal? Cltv { get; set; }

        public decimal? Hcltv { get; set; }

        public bool? Ftc { get; set; }

        public bool? FirstTimeHomebuyer { get; set; }

        public DateTime? EstimatedClosingDate { get; set; }

        public DateTime? ClosingDate { get; set; }

        public decimal? AppraisedValue { get; set; }

        public decimal? TotalLoanAmount { get; set; }

        public decimal? NoteRatePercent { get; set; }

        public string? PropertyAddressSummary { get; set; }

        public string? LockStatus { get; set; }

        public string? ProductName { get; set; }

        public string? LenderName { get; set; }

        public ManualLoanStructureEnrichmentResponse LoanStructure { get; set; } = new();

        public ManualTitleInfoResponse TitleInfo { get; set; } = new();

        public List<ManualDownPaymentSourceResponse> DownPaymentSources { get; set; } = new();

        public List<ManualPurchaseCreditResponse> PurchaseCredits { get; set; } = new();

        public List<ManualSubordinateLienResponse> SubordinateLiens { get; set; } = new();

        public ManualHousingExpenseInputsResponse HousingExpenseInputs { get; set; } = new();

        public List<ManualBorrowerApplicationSnapshotResponse> BorrowerApplications { get; set; } = new();
    }

    public class ManualBorrowerApplicationSnapshotResponse
    {
        public int ApplicationNumber { get; set; }

        public List<int> BorrowerIds { get; set; } = new();
    }

    public class ManualHousingExpenseInputsResponse
    {
        public ManualCalcInputResponse OtherFinancing { get; set; } = new();

        public ManualSourceCalcInputResponse Hoi { get; set; } = new();

        public ManualSourceCalcInputResponse Supplemental { get; set; } = new();

        public ManualSourceCalcInputResponse PropertyTaxes { get; set; } = new();

        public ManualMiInputResponse Mi { get; set; } = new();

        public ManualValueInputResponse AssociationDues { get; set; } = new();

        public ManualOtherInputResponse Other { get; set; } = new();
    }

    public class ManualCalcInputResponse
    {
        public bool IsCalc { get; set; }

        public string Factor { get; set; } = "Monthly";

        public string Value { get; set; } = string.Empty;
    }

    public class ManualSourceCalcInputResponse : ManualCalcInputResponse
    {
        public string Percent { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;
    }

    public class ManualMiInputResponse
    {
        public string Mode { get; set; } = string.Empty;

        public string Factor { get; set; } = "Monthly";

        public string Value { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public ManualMiRatesInputResponse MortgageInsurance { get; set; } = new();
    }

    public class ManualMiRatesInputResponse
    {
        public string InitialAnnualRate { get; set; } = string.Empty;

        public string InitialMonthly { get; set; } = string.Empty;

        public string RenewalAnnualRate { get; set; } = string.Empty;

        public string RenewalMonthly { get; set; } = string.Empty;
    }

    public class ManualValueInputResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class ManualOtherInputResponse : ManualValueInputResponse
    {
        public string Description { get; set; } = string.Empty;
    }

    public class ManualLoanSnapshotResponse
    {
        public string? LoanPurposeType { get; set; }

        public string? MortgageType { get; set; }

        public string? LienPriorityType { get; set; }

        public decimal NoteAmount { get; set; }

        public decimal? BaseLoanAmount { get; set; }

        public decimal? TotalLoanAmount { get; set; }

        public decimal? NoteRatePercent { get; set; }

        public decimal? QualifyingRate { get; set; }

        public int? LoanAmortizationPeriodCount { get; set; }

        public string? LoanAmortizationType { get; set; }

        public string? InterestRateBuydown { get; set; }

        public bool? InterestOnly { get; set; }

        public int? InterestOnlyTermMonths { get; set; }

        public string? ImpoundWaiver { get; set; }

        public int? LoanFico { get; set; }

        public bool? NoFico { get; set; }

        public int? ProjectedReserveMonths { get; set; }

        public decimal? Ltv { get; set; }

        public decimal? Cltv { get; set; }

        public decimal? Hcltv { get; set; }

        public string? RefinanceType { get; set; }

        public string? RefinanceProgram { get; set; }

        public decimal? ExistingLiensAmount { get; set; }

        public string Status { get; set; } = string.Empty;
    }

    public class ManualSubjectPropertySnapshotResponse
    {
        public string AddressLineText { get; set; } = string.Empty;

        public string CityName { get; set; } = string.Empty;

        public string StateCode { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string? UnitIdentifier { get; set; }

        public string? County { get; set; }

        public string? PropertyUsageType { get; set; }

        public string? PropertyType { get; set; }

        public string? ConstructionMethod { get; set; }

        public string? Occupancy { get; set; }

        public int FinancedUnitCount { get; set; }

        public bool? MixedUseProperty { get; set; }

        public bool? NonOccupantCoBorrower { get; set; }

        public bool? CommunityPropertyState { get; set; }

        public bool? EnergyImprovement { get; set; }

        public bool? SolarLienPriority { get; set; }

        public bool? ConversionOfContract { get; set; }

        public bool? Renovation { get; set; }

        public bool? ConstructionLoan { get; set; }

        public DateTime? LotAcquiredDate { get; set; }

        public decimal? OriginalCostOfLot { get; set; }

        public string? AttachmentType { get; set; }

        public string? StructureType { get; set; }

        public string? DesignType { get; set; }

        public int? YearBuilt { get; set; }

        public decimal? Acreage { get; set; }

        public string? Improvements { get; set; }

        public decimal? ImprovementCosts { get; set; }

        public int? AccessoryDwellingUnitCount { get; set; }

        public string? ConstructionStatus { get; set; }

        public string? ProjectName { get; set; }

        public string? CondoProjectManagerId { get; set; }

        public string? LegalDescription { get; set; }

        public decimal PropertyEstimatedValueAmount { get; set; }
    }

    public class ManualHousingExpenseSnapshotResponse
    {
        public string HousingExpenseType { get; set; } = string.Empty;

        public string ExpenseType => HousingExpenseType switch
        {
            "FirstMortgage" => "First Mortgage",
            "OtherFinancing" => "Other Financing",
            "PropertyTaxes" => "Property Taxes",
            "AssociationDues" => "Association Dues",
            _ => HousingExpenseType
        };

        public decimal PresentHousingExpenseAmount { get; set; }

        public decimal ProposedHousingExpenseAmount { get; set; }

        public decimal Amount => ProposedHousingExpenseAmount;

        public string? Mode { get; set; }

        public string? Frequency => Mode;

        public decimal? Factor { get; set; }

        public decimal? ValueAmount { get; set; }

        public string? SourceType { get; set; }

        public string? Description { get; set; }

        public decimal? InitialAnnualRate { get; set; }

        public decimal? InitialMonthlyAmount { get; set; }

        public decimal? RenewalAnnualRate { get; set; }

        public decimal? RenewalMonthlyAmount { get; set; }

        public bool? Included { get; set; }

        public bool IncludeInPiti => Included ?? true;

        public int DisplayOrder { get; set; }
    }

    public class ManualBorrowerSnapshotResponse
    {
        public int Id { get; set; }

        public string BorrowerType { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string? MiddleName { get; set; }

        public string LastName { get; set; } = string.Empty;

        public string? Suffix { get; set; }

        public string? Nickname { get; set; }

        public string? SsnItin { get; set; }

        public string? Email { get; set; }

        public string? CellPhone { get; set; }

        public string? HomePhone { get; set; }

        public string? WorkPhone { get; set; }

        public string? WorkPhoneExtension { get; set; }

        [JsonPropertyName("workPhoneExt")]
        public string? WorkPhoneExt
        {
            get => WorkPhoneExtension;
            set => WorkPhoneExtension = value;
        }

        public string? MaritalStatus { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public int? EstimatedCreditScore { get; set; }

        public bool EConsentAuthorized { get; set; }

        public bool CreditPullAuthorized { get; set; }

        public int? NumberOfDependents { get; set; }

        public string? DependentAges { get; set; }

        public bool? MailingAddressSameAsCurrent { get; set; }

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

        public string? OtherLanguageDescription { get; set; }

        [JsonPropertyName("languageOtherDescription")]
        public string? LanguageOtherDescription
        {
            get => OtherLanguageDescription;
            set => OtherLanguageDescription = value;
        }

        public JsonElement? LanguagePreferences { get; set; }

        public List<ManualBorrowerAddressSnapshotResponse> Addresses { get; set; } = new();

        public List<ManualBorrowerEmploymentSnapshotResponse> Employments { get; set; } = new();

        public List<ManualBorrowerIncomeSnapshotResponse> Incomes { get; set; } = new();

        public List<ManualBorrowerAssetSnapshotResponse> Assets { get; set; } = new();

        public List<ManualBorrowerLiabilitySnapshotResponse> Liabilities { get; set; } = new();

        public List<ManualBorrowerDeclarationSnapshotResponse> Declarations { get; set; } = new();

        public ManualGovernmentMonitoringSnapshotResponse? GovernmentMonitoring { get; set; }

        public ManualBorrowerMilitaryInfoSnapshotResponse? MilitaryInformation { get; set; }
    }

    public class ManualBorrowerAddressSnapshotResponse
    {
        public string AddressType { get; set; } = string.Empty;

        public string StreetAddress { get; set; } = string.Empty;

        public string? City { get; set; }

        public string? State { get; set; }

        public string? ZipCode { get; set; }

        public string? OccupancyType { get; set; }

        public int? YearsSpent { get; set; }

        public int? MonthsSpent { get; set; }
    }

    public class ManualBorrowerEmploymentSnapshotResponse
    {
        public string EmployerName { get; set; } = string.Empty;

        public string? EmploymentStatusType { get; set; }

        public DateTime? EmploymentStartDate { get; set; }

        public string? EmploymentPositionDescription { get; set; }
    }

    public class ManualBorrowerIncomeSnapshotResponse
    {
        public string IncomeType { get; set; } = string.Empty;

        public decimal CurrentIncomeMonthlyTotalAmount { get; set; }
    }

    public class ManualBorrowerAssetSnapshotResponse
    {
        public string AssetType { get; set; } = string.Empty;

        public decimal AssetCashOrMarketValueAmount { get; set; }
    }

    public class ManualBorrowerLiabilitySnapshotResponse
    {
        public string LiabilityType { get; set; } = string.Empty;

        public decimal MonthlyPaymentAmount { get; set; }

        public decimal UPBAmount { get; set; }
    }

    public class ManualBorrowerDeclarationSnapshotResponse
    {
        public string DeclarationType { get; set; } = string.Empty;

        public bool IsAffirmative { get; set; }

        public string? Explanation { get; set; }
    }

    public class ManualGovernmentMonitoringSnapshotResponse
    {
        public bool? ToBeCompletedByFinancialInstitution { get; set; }

        public string? EthnicityType { get; set; }

        public string? EthnicityOriginOther { get; set; }

        public string? RaceType { get; set; }

        public string? RaceOther { get; set; }

        public string? SexType { get; set; }

        public string? CollectionMethodType { get; set; }

        public bool? CollectedByVisualObservation { get; set; }

        public bool? CollectedEthnicityByObservation { get; set; }

        public bool? CollectedRaceByObservation { get; set; }

        public bool? CollectedSexByObservation { get; set; }

        public string? HmdaMonitoringType { get; set; }

        public bool? EthnicityVisualObservationIndicator { get; set; }

        public bool? RaceVisualObservationIndicator { get; set; }

        public bool? SexVisualObservationIndicator { get; set; }
    }

    public class ManualBorrowerMilitaryInfoSnapshotResponse
    {
        public string? MilitaryStatus { get; set; }

        public bool? ActiveDuty { get; set; }

        public bool? Veteran { get; set; }

        public bool? Reserves { get; set; }

        public bool? NationalGuard { get; set; }

        public string? ServiceBranch { get; set; }

        public DateTime? ServiceStartDate { get; set; }

        public DateTime? ServiceEndDate { get; set; }

        public DateTime? ProjectedExpirationDate { get; set; }

        public bool? SurvivorSpouse { get; set; }

        public bool? VaEligible { get; set; }

        public string? VaEligibilityStatus { get; set; }

        public bool? VaFirstTimeUse { get; set; }

        public bool? VaFundingFeeExempt { get; set; }
    }

    public class ManualLoanStructureEnrichmentResponse
    {
        public decimal? PurchasePrice { get; set; }

        public decimal? DownPaymentAmount { get; set; }

        public decimal? DownPaymentPercent { get; set; }

        public decimal? BaseLoanAmount { get; set; }

        public decimal? SubordinateLienAmount { get; set; }

        public decimal? QualifyingRate { get; set; }

        public bool? InterestOnly { get; set; }

        public string? ImpoundWaiver { get; set; }

        public decimal? PurchaseCredits { get; set; }

        public decimal? ProjectedReserveAmount { get; set; }

        public int? ProjectedReserveMonths { get; set; }

        public string? OwnerOfExistingMortgage { get; set; }

        public decimal? TotalLoanAmount { get; set; }

        public string? InterestRateBuydown { get; set; }

        public int? InterestOnlyTermMonths { get; set; }

        public int? LoanFico { get; set; }

        public bool? NoFico { get; set; }

        public decimal? Ltv { get; set; }

        public decimal? Cltv { get; set; }

        public decimal? Hcltv { get; set; }

        public string? RefinanceType { get; set; }

        public string? RefinanceProgram { get; set; }

        public decimal? ExistingLiensAmount { get; set; }

        public bool? LpaOffering { get; set; }

        public bool? AlternateDoc { get; set; }

        public bool? TemporaryBuydown { get; set; }

        public bool? BusinessPurpose { get; set; }

        public bool? BridgeLoan { get; set; }

        public bool? AssetDepletion { get; set; }

        public string? InvestmentIncomeMode { get; set; }

        public decimal? MonthlyGrossRentalIncome { get; set; }

        public decimal? RentalPercentage { get; set; }

        public decimal? TotalCalculatedRent { get; set; }

        public decimal? TotalExpenses { get; set; }

        public decimal? NetSubjectIncome { get; set; }

        public decimal? FirstMortgage { get; set; }

        public decimal? OtherFinancing { get; set; }

        public string? OtherFinancingMode { get; set; }

        public decimal? OtherFinancingFactor { get; set; }

        public decimal? OtherFinancingValue { get; set; }

        public decimal? Hoi { get; set; }

        public string? HoiMode { get; set; }

        public decimal? HoiFactor { get; set; }

        public string? HoiSource { get; set; }

        public decimal? Supplemental { get; set; }

        public string? SupplementalMode { get; set; }

        public decimal? SupplementalFactor { get; set; }

        public string? SupplementalSource { get; set; }

        public decimal? PropertyTaxes { get; set; }

        public string? PropertyTaxesMode { get; set; }

        public decimal? PropertyTaxesFactor { get; set; }

        public string? PropertyTaxesSource { get; set; }

        public string? MiMode { get; set; }

        public decimal? MiInitialAnnualRate { get; set; }

        public decimal? MiInitialMonthly { get; set; }

        public decimal? MiRenewalAnnualRate { get; set; }

        public decimal? MiRenewalMonthly { get; set; }

        public decimal? AssociationDues { get; set; }

        public string? OtherDescription { get; set; }

        public decimal? OtherAmount { get; set; }

        public decimal? TotalPiti { get; set; }

        public string? AusRunType { get; set; }

        public string? AusStatus { get; set; }

        public string? RecommendedAusProvider { get; set; }

        public string? DuRecommendation { get; set; }

        public string? DuEligibility { get; set; }

        public string? DuRiskClass { get; set; }

        public string? LpaRecommendation { get; set; }

        public string? LpaEligibility { get; set; }

        public string? LpaRiskClass { get; set; }
    }

    public class ManualTitleInfoResponse
    {
        public string? Vesting { get; set; }

        public string? MannerTitleHeld { get; set; }

        public string? TitleHeldInNameOf { get; set; }

        public string? VestingToRead { get; set; }

        public string? TrustInformation { get; set; }

        public string? IndianCountryLandTenure { get; set; }

        public string? EstateHeldIn { get; set; }

        public string? TitleCompany { get; set; }

        public string? TitleInsuranceCompany { get; set; }

        public string? TitleCommitmentNumber { get; set; }

        public string? TitlePolicyNumber { get; set; }

        public string? VestingEntityType { get; set; }

        public string? TitleNotes { get; set; }

        public bool Homestead { get; set; }

        [JsonPropertyName("homesteadExemption")]
        public bool HomesteadExemption => Homestead;

        public bool TitleCurative { get; set; }

        [JsonPropertyName("titleCurativeRequired")]
        public bool TitleCurativeRequired => TitleCurative;

        public bool PowerOfAttorney { get; set; }

        public bool RecordedEasement { get; set; }

        public string? TitleOfficer { get; set; }

        public string? TitleContact { get; set; }

        public string? EscrowCompany { get; set; }

        public string? SettlementAgent { get; set; }
    }

    public class ManualDownPaymentSourceResponse
    {
        public int Id { get; set; }

        public string SourceType { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public bool Included { get; set; }

        public int DisplayOrder { get; set; }
    }

    public class ManualPurchaseCreditResponse
    {
        public int Id { get; set; }

        public string CreditType { get; set; } = string.Empty;

        public string SourceType { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public int DisplayOrder { get; set; }
    }

    public class ManualSubordinateLienResponse
    {
        public int Id { get; set; }

        public string LienType { get; set; } = string.Empty;

        [JsonPropertyName("lienPosition")]
        public string LienPosition => LienType;

        public string? LienName { get; set; }

        [JsonPropertyName("creditorName")]
        public string? CreditorName => LienName;

        public decimal? MonthlyPaymentAmount { get; set; }

        [JsonPropertyName("monthlyPayment")]
        public decimal? MonthlyPayment => MonthlyPaymentAmount;

        public decimal? LoanAmount { get; set; }

        public string? SourceType { get; set; }

        [JsonPropertyName("fundSourceType")]
        public string? FundSourceType => SourceType;

        public int DisplayOrder { get; set; }
    }
}