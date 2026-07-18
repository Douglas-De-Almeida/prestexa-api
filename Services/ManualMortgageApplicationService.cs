using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;
using PrestexaAPI.Models.Responses;
using PrestexaAPI.Services.Contacts;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace PrestexaAPI.Services
{
    public class ManualMortgageApplicationService
    {
        private readonly AppDbContext _context;
        private readonly IContactService _contactService;

        public ManualMortgageApplicationService(AppDbContext context, IContactService contactService)
        {
            _context = context;
            _contactService = contactService;
        }

        public async Task<ManualMortgageApplicationResponse> CreateAsync(
            ManualMortgageApplicationRequest request,
            int userId,
            string companyNmlsNumber,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var loan = new Loan
            {
                LoanNumber = await GenerateLoanNumberAsync(cancellationToken),
                CompanyNmlsNumber = companyNmlsNumber,
                UserId = userId,
                Subject_Street_Address = request.SubjectProperty.AddressLineText,
                Subject_City = request.SubjectProperty.CityName,
                Subject_State = request.SubjectProperty.StateCode,
                Subject_ZipCode = request.SubjectProperty.PostalCode,
                LoanAmount = request.Loan.TotalLoanAmount ?? request.LoanStructure?.TotalLoanAmount ?? request.Loan.NoteAmount,
                Status = LoanStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync(cancellationToken);

            await SaveAggregateAsync(loan, request, companyNmlsNumber, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return await BuildApplicationResponseAsync(loan.LoanNumber, companyNmlsNumber, cancellationToken)
                ?? throw new InvalidOperationException("Unable to load created application.");
        }

        public async Task<ManualMortgageApplicationResponse?> GetAsync(
            string loanNumber,
            string companyNmlsNumber,
            CancellationToken cancellationToken = default)
        {
            return await BuildApplicationResponseAsync(loanNumber, companyNmlsNumber, cancellationToken);
        }

        public async Task<ManualMortgageApplicationResponse?> UpdateAsync(
            string loanNumber,
            ManualMortgageApplicationRequest request,
            string companyNmlsNumber,
            int actorUserId,
            string? actorName,
            string? actorRole,
            CancellationToken cancellationToken = default)
        {
            var loan = await _context.Loans
                .FirstOrDefaultAsync(x => x.LoanNumber == loanNumber && x.CompanyNmlsNumber == companyNmlsNumber, cancellationToken);

            if (loan == null)
                return null;

            var beforeSnapshot = await BuildApplicationResponseAsync(loanNumber, companyNmlsNumber, cancellationToken)
                ?? throw new InvalidOperationException("Unable to load current loan application before update.");

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            loan.Subject_Street_Address = request.SubjectProperty.AddressLineText;
            loan.Subject_City = request.SubjectProperty.CityName;
            loan.Subject_State = request.SubjectProperty.StateCode;
            loan.Subject_ZipCode = request.SubjectProperty.PostalCode;
            loan.LoanAmount = request.Loan.TotalLoanAmount ?? request.LoanStructure?.TotalLoanAmount ?? request.Loan.NoteAmount;
            loan.UpdatedAt = DateTime.UtcNow;

            await ClearAggregateAsync(loan.Id, companyNmlsNumber, cancellationToken, includeBorrowers: false);
            await SaveAggregateAsync(loan, request, companyNmlsNumber, cancellationToken, includeBorrowers: false);
            var borrowerApplicationAssignments = BuildBorrowerApplicationAssignments(request.BorrowerApplications);
            await SyncBorrowersAsync(loan, request.Borrowers, borrowerApplicationAssignments, companyNmlsNumber, actorUserId, cancellationToken);

            var afterSnapshot = await BuildApplicationResponseAsync(loanNumber, companyNmlsNumber, cancellationToken)
                ?? throw new InvalidOperationException("Unable to load updated loan application.");

            await AppendLoanAuditTrailAsync(
                loan,
                beforeSnapshot,
                afterSnapshot,
                actorUserId,
                actorName,
                actorRole,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return afterSnapshot;
        }

        private async Task AppendLoanAuditTrailAsync(
            Loan loan,
            ManualMortgageApplicationResponse beforeSnapshot,
            ManualMortgageApplicationResponse afterSnapshot,
            int actorUserId,
            string? actorName,
            string? actorRole,
            CancellationToken cancellationToken)
        {
            var activities = new List<LoanActivity>();

            AddLoanStructureAuditActivities(activities, loan, beforeSnapshot, afterSnapshot, actorUserId, actorName, actorRole);
            AddPropertyAuditActivities(activities, loan, beforeSnapshot, afterSnapshot, actorUserId, actorName, actorRole);
            AddTitleAuditActivities(activities, loan, beforeSnapshot, afterSnapshot, actorUserId, actorName, actorRole);
            AddBorrowerAuditActivities(activities, loan, beforeSnapshot, afterSnapshot, actorUserId, actorName, actorRole);
            AddCollectionAuditActivities(
                activities,
                loan,
                "Down Payment Sources",
                LoanActivityType.DownPaymentSourceChange,
                beforeSnapshot.DownPaymentSources,
                afterSnapshot.DownPaymentSources,
                source => source.DisplayOrder,
                source => string.IsNullOrWhiteSpace(source.SourceType) ? "Down Payment Source" : source.SourceType.Trim(),
                () => new (string PropertyName, string Label)[]
                {
                    (nameof(ManualDownPaymentSourceResponse.SourceType), "Source Type"),
                    (nameof(ManualDownPaymentSourceResponse.Amount), "Amount"),
                    (nameof(ManualDownPaymentSourceResponse.Included), "Included")
                },
                actorUserId,
                actorName,
                actorRole);

            AddCollectionAuditActivities(
                activities,
                loan,
                "Purchase Credits",
                LoanActivityType.PurchaseCreditChange,
                beforeSnapshot.PurchaseCredits,
                afterSnapshot.PurchaseCredits,
                credit => credit.DisplayOrder,
                credit => string.IsNullOrWhiteSpace(credit.CreditType) ? "Purchase Credit" : credit.CreditType.Trim(),
                () => new (string PropertyName, string Label)[]
                {
                    (nameof(ManualPurchaseCreditResponse.CreditType), "Credit Type"),
                    (nameof(ManualPurchaseCreditResponse.SourceType), "Source Type"),
                    (nameof(ManualPurchaseCreditResponse.Amount), "Amount")
                },
                actorUserId,
                actorName,
                actorRole);

            AddCollectionAuditActivities(
                activities,
                loan,
                "Subordinate Liens",
                LoanActivityType.SubordinateLienChange,
                beforeSnapshot.SubordinateLiens,
                afterSnapshot.SubordinateLiens,
                lien => lien.DisplayOrder,
                lien => string.IsNullOrWhiteSpace(lien.LienType) ? "Subordinate Lien" : lien.LienType.Trim(),
                () => new (string PropertyName, string Label)[]
                {
                    (nameof(ManualSubordinateLienResponse.LienType), "Lien Type"),
                    (nameof(ManualSubordinateLienResponse.LienName), "Lien Name"),
                    (nameof(ManualSubordinateLienResponse.MonthlyPaymentAmount), "Monthly Payment"),
                    (nameof(ManualSubordinateLienResponse.LoanAmount), "Loan Amount"),
                    (nameof(ManualSubordinateLienResponse.SourceType), "Source Type")
                },
                actorUserId,
                actorName,
                actorRole);

            AddCollectionAuditActivities(
                activities,
                loan,
                "Housing Expenses",
                LoanActivityType.HousingExpenseChange,
                beforeSnapshot.HousingExpenses,
                afterSnapshot.HousingExpenses,
                expense => expense.DisplayOrder,
                expense => string.IsNullOrWhiteSpace(expense.ExpenseType) ? expense.HousingExpenseType : expense.ExpenseType,
                () => new (string PropertyName, string Label)[]
                {
                    (nameof(ManualHousingExpenseSnapshotResponse.HousingExpenseType), "Housing Expense Type"),
                    (nameof(ManualHousingExpenseSnapshotResponse.PresentHousingExpenseAmount), "Present Amount"),
                    (nameof(ManualHousingExpenseSnapshotResponse.ProposedHousingExpenseAmount), "Proposed Amount"),
                    (nameof(ManualHousingExpenseSnapshotResponse.Mode), "Mode"),
                    (nameof(ManualHousingExpenseSnapshotResponse.Factor), "Factor"),
                    (nameof(ManualHousingExpenseSnapshotResponse.ValueAmount), "Value"),
                    (nameof(ManualHousingExpenseSnapshotResponse.SourceType), "Source Type"),
                    (nameof(ManualHousingExpenseSnapshotResponse.Description), "Description"),
                    (nameof(ManualHousingExpenseSnapshotResponse.InitialAnnualRate), "Initial Annual Rate"),
                    (nameof(ManualHousingExpenseSnapshotResponse.InitialMonthlyAmount), "Initial Monthly Amount"),
                    (nameof(ManualHousingExpenseSnapshotResponse.RenewalAnnualRate), "Renewal Annual Rate"),
                    (nameof(ManualHousingExpenseSnapshotResponse.RenewalMonthlyAmount), "Renewal Monthly Amount"),
                    (nameof(ManualHousingExpenseSnapshotResponse.Included), "Included")
                },
                actorUserId,
                actorName,
                actorRole);

            if (activities.Count == 0)
                return;

            _context.LoanActivities.AddRange(activities);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private void AddLoanStructureAuditActivities(
            List<LoanActivity> activities,
            Loan loan,
            ManualMortgageApplicationResponse beforeSnapshot,
            ManualMortgageApplicationResponse afterSnapshot,
            int actorUserId,
            string? actorName,
            string? actorRole)
        {
            var changes = CompareFields(beforeSnapshot.LoanStructure, afterSnapshot.LoanStructure, new (string PropertyName, string Label)[]
            {
                (nameof(ManualLoanStructureEnrichmentResponse.PurchasePrice), "Purchase Price"),
                (nameof(ManualLoanStructureEnrichmentResponse.DownPaymentAmount), "Down Payment Amount"),
                (nameof(ManualLoanStructureEnrichmentResponse.DownPaymentPercent), "Down Payment Percent"),
                (nameof(ManualLoanStructureEnrichmentResponse.BaseLoanAmount), "Base Loan Amount"),
                (nameof(ManualLoanStructureEnrichmentResponse.SubordinateLienAmount), "Subordinate Lien Amount"),
                (nameof(ManualLoanStructureEnrichmentResponse.QualifyingRate), "Qualifying Rate"),
                (nameof(ManualLoanStructureEnrichmentResponse.InterestOnly), "Interest Only"),
                (nameof(ManualLoanStructureEnrichmentResponse.ImpoundWaiver), "Impound Waiver"),
                (nameof(ManualLoanStructureEnrichmentResponse.PurchaseCredits), "Purchase Credits"),
                (nameof(ManualLoanStructureEnrichmentResponse.ProjectedReserveAmount), "Projected Reserve Amount"),
                (nameof(ManualLoanStructureEnrichmentResponse.ProjectedReserveMonths), "Projected Reserve Months"),
                (nameof(ManualLoanStructureEnrichmentResponse.OwnerOfExistingMortgage), "Owner of Existing Mortgage"),
                (nameof(ManualLoanStructureEnrichmentResponse.TotalLoanAmount), "Loan Amount"),
                (nameof(ManualLoanStructureEnrichmentResponse.InterestRateBuydown), "Interest Rate Buydown"),
                (nameof(ManualLoanStructureEnrichmentResponse.InterestOnlyTermMonths), "Interest Only Term Months"),
                (nameof(ManualLoanStructureEnrichmentResponse.LoanFico), "Loan FICO"),
                (nameof(ManualLoanStructureEnrichmentResponse.NoFico), "No FICO"),
                (nameof(ManualLoanStructureEnrichmentResponse.Ltv), "LTV"),
                (nameof(ManualLoanStructureEnrichmentResponse.Cltv), "CLTV"),
                (nameof(ManualLoanStructureEnrichmentResponse.Hcltv), "HCLTV"),
                (nameof(ManualLoanStructureEnrichmentResponse.RefinanceType), "Refinance Type"),
                (nameof(ManualLoanStructureEnrichmentResponse.RefinanceProgram), "Refinance Program"),
                (nameof(ManualLoanStructureEnrichmentResponse.ExistingLiensAmount), "Existing Liens Amount"),
                (nameof(ManualLoanStructureEnrichmentResponse.LpaOffering), "LPA Offering"),
                (nameof(ManualLoanStructureEnrichmentResponse.AlternateDoc), "Alternate Doc"),
                (nameof(ManualLoanStructureEnrichmentResponse.TemporaryBuydown), "Temporary Buydown"),
                (nameof(ManualLoanStructureEnrichmentResponse.BusinessPurpose), "Business Purpose"),
                (nameof(ManualLoanStructureEnrichmentResponse.BridgeLoan), "Bridge Loan"),
                (nameof(ManualLoanStructureEnrichmentResponse.AssetDepletion), "Asset Depletion"),
                (nameof(ManualLoanStructureEnrichmentResponse.InvestmentIncomeMode), "Investment Income Mode"),
                (nameof(ManualLoanStructureEnrichmentResponse.MonthlyGrossRentalIncome), "Monthly Gross Rental Income"),
                (nameof(ManualLoanStructureEnrichmentResponse.RentalPercentage), "Rental Percentage"),
                (nameof(ManualLoanStructureEnrichmentResponse.TotalCalculatedRent), "Total Calculated Rent"),
                (nameof(ManualLoanStructureEnrichmentResponse.TotalExpenses), "Total Expenses"),
                (nameof(ManualLoanStructureEnrichmentResponse.NetSubjectIncome), "Net Subject Income")
            });

            if (changes.Count == 0)
                return;

            AddLoanActivity(
                activities,
                loan,
                LoanActivityType.LoanStructureChange,
                "Loan Structure",
                changes.Count == 1 && string.Equals(changes[0].Label, "Loan Amount", StringComparison.OrdinalIgnoreCase)
                    ? "Loan Amount updated"
                    : "Loan Structure updated",
                changes,
                actorUserId,
                actorName,
                actorRole,
                SerializeAuditValue(beforeSnapshot.LoanStructure),
                SerializeAuditValue(afterSnapshot.LoanStructure));
        }

        private void AddPropertyAuditActivities(
            List<LoanActivity> activities,
            Loan loan,
            ManualMortgageApplicationResponse beforeSnapshot,
            ManualMortgageApplicationResponse afterSnapshot,
            int actorUserId,
            string? actorName,
            string? actorRole)
        {
            if (beforeSnapshot.SubjectProperty == null || afterSnapshot.SubjectProperty == null)
                return;

            var changes = CompareFields(beforeSnapshot.SubjectProperty, afterSnapshot.SubjectProperty, new (string PropertyName, string Label)[]
            {
                (nameof(ManualSubjectPropertySnapshotResponse.AddressLineText), "Address"),
                (nameof(ManualSubjectPropertySnapshotResponse.UnitIdentifier), "Unit"),
                (nameof(ManualSubjectPropertySnapshotResponse.CityName), "City"),
                (nameof(ManualSubjectPropertySnapshotResponse.StateCode), "State"),
                (nameof(ManualSubjectPropertySnapshotResponse.PostalCode), "Postal Code"),
                (nameof(ManualSubjectPropertySnapshotResponse.County), "County"),
                (nameof(ManualSubjectPropertySnapshotResponse.PropertyUsageType), "Property Usage"),
                (nameof(ManualSubjectPropertySnapshotResponse.PropertyType), "Property Type"),
                (nameof(ManualSubjectPropertySnapshotResponse.ConstructionMethod), "Construction Method"),
                (nameof(ManualSubjectPropertySnapshotResponse.Occupancy), "Occupancy"),
                (nameof(ManualSubjectPropertySnapshotResponse.FinancedUnitCount), "Financed Unit Count"),
                (nameof(ManualSubjectPropertySnapshotResponse.MixedUseProperty), "Mixed Use Property"),
                (nameof(ManualSubjectPropertySnapshotResponse.NonOccupantCoBorrower), "Non-Occupant Co-Borrower"),
                (nameof(ManualSubjectPropertySnapshotResponse.CommunityPropertyState), "Community Property State"),
                (nameof(ManualSubjectPropertySnapshotResponse.EnergyImprovement), "Energy Improvement"),
                (nameof(ManualSubjectPropertySnapshotResponse.SolarLienPriority), "Solar Lien Priority"),
                (nameof(ManualSubjectPropertySnapshotResponse.ConversionOfContract), "Conversion of Contract"),
                (nameof(ManualSubjectPropertySnapshotResponse.Renovation), "Renovation"),
                (nameof(ManualSubjectPropertySnapshotResponse.ConstructionLoan), "Construction Loan"),
                (nameof(ManualSubjectPropertySnapshotResponse.LotAcquiredDate), "Lot Acquired Date"),
                (nameof(ManualSubjectPropertySnapshotResponse.OriginalCostOfLot), "Original Cost of Lot"),
                (nameof(ManualSubjectPropertySnapshotResponse.AttachmentType), "Attachment Type"),
                (nameof(ManualSubjectPropertySnapshotResponse.StructureType), "Structure Type"),
                (nameof(ManualSubjectPropertySnapshotResponse.DesignType), "Design Type"),
                (nameof(ManualSubjectPropertySnapshotResponse.YearBuilt), "Year Built"),
                (nameof(ManualSubjectPropertySnapshotResponse.Acreage), "Acreage"),
                (nameof(ManualSubjectPropertySnapshotResponse.Improvements), "Improvements"),
                (nameof(ManualSubjectPropertySnapshotResponse.ImprovementCosts), "Improvement Costs"),
                (nameof(ManualSubjectPropertySnapshotResponse.AccessoryDwellingUnitCount), "Accessory Dwelling Unit Count"),
                (nameof(ManualSubjectPropertySnapshotResponse.ConstructionStatus), "Construction Status"),
                (nameof(ManualSubjectPropertySnapshotResponse.ProjectName), "Project Name"),
                (nameof(ManualSubjectPropertySnapshotResponse.CondoProjectManagerId), "Condo Project Manager ID"),
                (nameof(ManualSubjectPropertySnapshotResponse.LegalDescription), "Legal Description"),
                (nameof(ManualSubjectPropertySnapshotResponse.PropertyEstimatedValueAmount), "Property Value")
            });

            if (changes.Count == 0)
                return;

            AddLoanActivity(
                activities,
                loan,
                LoanActivityType.PropertyChange,
                "Property",
                changes.Count == 1 && string.Equals(changes[0].Label, "Property Value", StringComparison.OrdinalIgnoreCase)
                    ? "Property value updated"
                    : "Property updated",
                changes,
                actorUserId,
                actorName,
                actorRole,
                SerializeAuditValue(beforeSnapshot.SubjectProperty),
                SerializeAuditValue(afterSnapshot.SubjectProperty));
        }

        private void AddTitleAuditActivities(
            List<LoanActivity> activities,
            Loan loan,
            ManualMortgageApplicationResponse beforeSnapshot,
            ManualMortgageApplicationResponse afterSnapshot,
            int actorUserId,
            string? actorName,
            string? actorRole)
        {
            var changes = CompareFields(beforeSnapshot.TitleInfo, afterSnapshot.TitleInfo, new (string PropertyName, string Label)[]
            {
                (nameof(ManualTitleInfoResponse.Vesting), "Vesting"),
                (nameof(ManualTitleInfoResponse.MannerTitleHeld), "Manner Title Held"),
                (nameof(ManualTitleInfoResponse.TitleHeldInNameOf), "Title Held in Name Of"),
                (nameof(ManualTitleInfoResponse.VestingToRead), "Vesting To Read"),
                (nameof(ManualTitleInfoResponse.TrustInformation), "Trust Information"),
                (nameof(ManualTitleInfoResponse.IndianCountryLandTenure), "Indian Country Land Tenure"),
                (nameof(ManualTitleInfoResponse.EstateHeldIn), "Estate Held In"),
                (nameof(ManualTitleInfoResponse.TitleCompany), "Title Company"),
                (nameof(ManualTitleInfoResponse.TitleInsuranceCompany), "Title Insurance Company"),
                (nameof(ManualTitleInfoResponse.TitleCommitmentNumber), "Title Commitment Number"),
                (nameof(ManualTitleInfoResponse.TitlePolicyNumber), "Title Policy Number"),
                (nameof(ManualTitleInfoResponse.VestingEntityType), "Vesting Entity Type"),
                (nameof(ManualTitleInfoResponse.TitleNotes), "Title Notes"),
                (nameof(ManualTitleInfoResponse.Homestead), "Homestead"),
                (nameof(ManualTitleInfoResponse.TitleCurative), "Title Curative"),
                (nameof(ManualTitleInfoResponse.PowerOfAttorney), "Power of Attorney"),
                (nameof(ManualTitleInfoResponse.RecordedEasement), "Recorded Easement"),
                (nameof(ManualTitleInfoResponse.TitleOfficer), "Title Officer"),
                (nameof(ManualTitleInfoResponse.TitleContact), "Title Contact"),
                (nameof(ManualTitleInfoResponse.EscrowCompany), "Escrow Company"),
                (nameof(ManualTitleInfoResponse.SettlementAgent), "Settlement Agent")
            });

            if (changes.Count == 0)
                return;

            AddLoanActivity(
                activities,
                loan,
                LoanActivityType.TitleChange,
                "Title",
                changes.Count == 1 ? $"{changes[0].Label} updated" : "Title updated",
                changes,
                actorUserId,
                actorName,
                actorRole,
                SerializeAuditValue(beforeSnapshot.TitleInfo),
                SerializeAuditValue(afterSnapshot.TitleInfo));
        }

        private void AddBorrowerAuditActivities(
            List<LoanActivity> activities,
            Loan loan,
            ManualMortgageApplicationResponse beforeSnapshot,
            ManualMortgageApplicationResponse afterSnapshot,
            int actorUserId,
            string? actorName,
            string? actorRole)
        {
            var beforeGroups = beforeSnapshot.Borrowers
                .GroupBy(x => NormalizeBorrowerType(x.BorrowerType), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

            var afterGroups = afterSnapshot.Borrowers
                .GroupBy(x => NormalizeBorrowerType(x.BorrowerType), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

            var borrowerTypes = beforeGroups.Keys
                .Union(afterGroups.Keys, StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetBorrowerSortOrder)
                .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var borrowerType in borrowerTypes)
            {
                beforeGroups.TryGetValue(borrowerType, out var beforeBorrowers);
                afterGroups.TryGetValue(borrowerType, out var afterBorrowers);

                beforeBorrowers ??= [];
                afterBorrowers ??= [];

                var maxCount = Math.Max(beforeBorrowers.Count, afterBorrowers.Count);

                for (var index = 0; index < maxCount; index++)
                {
                    var beforeBorrower = index < beforeBorrowers.Count ? beforeBorrowers[index] : null;
                    var afterBorrower = index < afterBorrowers.Count ? afterBorrowers[index] : null;

                    if (beforeBorrower == null && afterBorrower == null)
                        continue;

                    var borrowerLabel = GetBorrowerDisplayLabel(afterBorrower?.BorrowerType ?? beforeBorrower?.BorrowerType ?? borrowerType, index);

                    if (beforeBorrower == null)
                    {
                        AddLoanActivity(
                            activities,
                            loan,
                            LoanActivityType.BorrowerChange,
                            "Borrowers",
                            $"{borrowerLabel} added",
                            [],
                            actorUserId,
                            actorName,
                            actorRole,
                            null,
                            SerializeAuditValue(afterBorrower));
                        continue;
                    }

                    if (afterBorrower == null)
                    {
                        AddLoanActivity(
                            activities,
                            loan,
                            LoanActivityType.BorrowerChange,
                            "Borrowers",
                            $"{borrowerLabel} removed",
                            [],
                            actorUserId,
                            actorName,
                            actorRole,
                            SerializeAuditValue(beforeBorrower),
                            null);
                        continue;
                    }

                    var changes = CompareFields(beforeBorrower, afterBorrower, new (string PropertyName, string Label)[]
                    {
                        (nameof(ManualBorrowerSnapshotResponse.FirstName), "First Name"),
                        (nameof(ManualBorrowerSnapshotResponse.MiddleName), "Middle Name"),
                        (nameof(ManualBorrowerSnapshotResponse.LastName), "Last Name"),
                        (nameof(ManualBorrowerSnapshotResponse.Suffix), "Suffix"),
                        (nameof(ManualBorrowerSnapshotResponse.Nickname), "Nickname"),
                        (nameof(ManualBorrowerSnapshotResponse.SsnItin), "SSN / ITIN"),
                        (nameof(ManualBorrowerSnapshotResponse.Email), "Email"),
                        (nameof(ManualBorrowerSnapshotResponse.CellPhone), "Cell Phone"),
                        (nameof(ManualBorrowerSnapshotResponse.HomePhone), "Home Phone"),
                        (nameof(ManualBorrowerSnapshotResponse.WorkPhone), "Work Phone"),
                        (nameof(ManualBorrowerSnapshotResponse.MaritalStatus), "Marital Status"),
                        (nameof(ManualBorrowerSnapshotResponse.DateOfBirth), "Date of Birth"),
                        (nameof(ManualBorrowerSnapshotResponse.EstimatedCreditScore), "Estimated Credit Score"),
                        (nameof(ManualBorrowerSnapshotResponse.NumberOfDependents), "Number of Dependents"),
                        (nameof(ManualBorrowerSnapshotResponse.DependentAges), "Dependent Ages"),
                        (nameof(ManualBorrowerSnapshotResponse.LanguagePreferences), "Language Preferences"),
                        (nameof(ManualBorrowerSnapshotResponse.EConsentAuthorized), "E-Consent Authorized"),
                        (nameof(ManualBorrowerSnapshotResponse.CreditPullAuthorized), "Credit Pull Authorized")
                    });

                    AddCountChange(changes, "Addresses", beforeBorrower.Addresses.Count, afterBorrower.Addresses.Count);
                    AddCountChange(changes, "Employments", beforeBorrower.Employments.Count, afterBorrower.Employments.Count);
                    AddCountChange(changes, "Incomes", beforeBorrower.Incomes.Count, afterBorrower.Incomes.Count);
                    AddCountChange(changes, "Assets", beforeBorrower.Assets.Count, afterBorrower.Assets.Count);
                    AddCountChange(changes, "Liabilities", beforeBorrower.Liabilities.Count, afterBorrower.Liabilities.Count);
                    AddCountChange(changes, "Declarations", beforeBorrower.Declarations.Count, afterBorrower.Declarations.Count);

                    var beforeGov = SerializeAuditValue(beforeBorrower.GovernmentMonitoring);
                    var afterGov = SerializeAuditValue(afterBorrower.GovernmentMonitoring);
                    if (!string.Equals(beforeGov, afterGov, StringComparison.Ordinal))
                        changes.Add(new AuditFieldChange("Government Monitoring", beforeGov, afterGov));

                    var beforeMilitary = SerializeAuditValue(beforeBorrower.MilitaryInformation);
                    var afterMilitary = SerializeAuditValue(afterBorrower.MilitaryInformation);
                    if (!string.Equals(beforeMilitary, afterMilitary, StringComparison.Ordinal))
                        changes.Add(new AuditFieldChange("Military Information", beforeMilitary, afterMilitary));

                    if (changes.Count == 0)
                        continue;

                    var summary = changes.Count == 1 && string.Equals(changes[0].Label, "Email", StringComparison.OrdinalIgnoreCase)
                        ? $"{borrowerLabel} email updated"
                        : $"{borrowerLabel} updated";

                    AddLoanActivity(
                        activities,
                        loan,
                        LoanActivityType.BorrowerChange,
                        "Borrowers",
                        summary,
                        changes,
                        actorUserId,
                        actorName,
                        actorRole,
                        SerializeAuditValue(beforeBorrower),
                        SerializeAuditValue(afterBorrower));
                }
            }
        }

        private void AddCollectionAuditActivities<T>(
            List<LoanActivity> activities,
            Loan loan,
            string section,
            LoanActivityType activityType,
            IReadOnlyCollection<T> beforeItems,
            IReadOnlyCollection<T> afterItems,
            Func<T, int> keySelector,
            Func<T, string> rowLabelSelector,
            Func<(string PropertyName, string Label)[]> fieldSelector,
            int actorUserId,
            string? actorName,
            string? actorRole)
        {
            var beforeByKey = beforeItems
                .GroupBy(keySelector)
                .ToDictionary(group => group.Key, group => group.First());

            var afterByKey = afterItems
                .GroupBy(keySelector)
                .ToDictionary(group => group.Key, group => group.First());

            var keys = beforeByKey.Keys
                .Union(afterByKey.Keys)
                .OrderBy(x => x)
                .ToList();

            foreach (var key in keys)
            {
                beforeByKey.TryGetValue(key, out var beforeItem);
                afterByKey.TryGetValue(key, out var afterItem);

                if (beforeItem == null && afterItem == null)
                    continue;

                if (beforeItem == null)
                {
                    var afterLabel = rowLabelSelector(afterItem!);
                    AddLoanActivity(
                        activities,
                        loan,
                        activityType,
                        section,
                        $"{afterLabel} added",
                        [],
                        actorUserId,
                        actorName,
                        actorRole,
                        null,
                        SerializeAuditValue(afterItem));
                    continue;
                }

                if (afterItem == null)
                {
                    var beforeLabel = rowLabelSelector(beforeItem);
                    AddLoanActivity(
                        activities,
                        loan,
                        activityType,
                        section,
                        $"{beforeLabel} removed",
                        [],
                        actorUserId,
                        actorName,
                        actorRole,
                        SerializeAuditValue(beforeItem),
                        null);
                    continue;
                }

                var changes = CompareFields(beforeItem, afterItem, fieldSelector());
                if (changes.Count == 0)
                    continue;

                var label = rowLabelSelector(afterItem);
                var summary = changes.Count == 1
                    ? $"{label} {changes[0].Label.ToLowerInvariant()} updated"
                    : $"{label} updated";

                AddLoanActivity(
                    activities,
                    loan,
                    activityType,
                    section,
                    summary,
                    changes,
                    actorUserId,
                    actorName,
                    actorRole,
                    SerializeAuditValue(beforeItem),
                    SerializeAuditValue(afterItem));
            }
        }

        private void AddLoanActivity(
            List<LoanActivity> activities,
            Loan loan,
            LoanActivityType activityType,
            string section,
            string summary,
            IReadOnlyCollection<AuditFieldChange> changes,
            int actorUserId,
            string? actorName,
            string? actorRole,
            string? oldValue = null,
            string? newValue = null)
        {
            var normalizedActorName = string.IsNullOrWhiteSpace(actorName)
                ? $"User {actorUserId}"
                : actorName.Trim();

            var normalizedActorRole = string.IsNullOrWhiteSpace(actorRole)
                ? null
                : actorRole.Trim();

            var metadata = JsonSerializer.Serialize(new
            {
                section,
                summary,
                changeType = activityType.ToString(),
                oldValue,
                newValue,
                details = changes.Select(change => new
                {
                    field = change.Label,
                    oldValue = change.OldValue,
                    newValue = change.NewValue
                }).ToList()
            });

            activities.Add(new LoanActivity
            {
                LoanId = loan.Id,
                LoanNumber = loan.LoanNumber,
                CompanyNmlsNumber = loan.CompanyNmlsNumber,
                ParentActivityId = null,
                ActivityType = activityType,
                Message = summary,
                MetadataJson = metadata,
                NotifyLoanTeam = false,
                Visibility = LoanActivityVisibility.InternalOnly,
                ActorUserId = actorUserId,
                ActorName = normalizedActorName,
                ActorRole = normalizedActorRole,
                ActorType = LoanActivityActorType.User,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        private static List<AuditFieldChange> CompareFields(object? beforeValue, object? afterValue, IReadOnlyCollection<(string PropertyName, string Label)> fields)
        {
            var changes = new List<AuditFieldChange>();

            foreach (var (propertyName, label) in fields)
            {
                var beforeField = GetPropertyValue(beforeValue, propertyName);
                var afterField = GetPropertyValue(afterValue, propertyName);

                if (string.Equals(FormatAuditValue(beforeField), FormatAuditValue(afterField), StringComparison.Ordinal))
                    continue;

                changes.Add(new AuditFieldChange(label, FormatAuditValue(beforeField), FormatAuditValue(afterField)));
            }

            return changes;
        }

        private static void AddCountChange(List<AuditFieldChange> changes, string label, int beforeCount, int afterCount)
        {
            if (beforeCount == afterCount)
                return;

            changes.Add(new AuditFieldChange(label, beforeCount.ToString(CultureInfo.InvariantCulture), afterCount.ToString(CultureInfo.InvariantCulture)));
        }

        private static object? GetPropertyValue(object? instance, string propertyName)
        {
            if (instance == null)
                return null;

            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            return property?.GetValue(instance);
        }

        private static string? SerializeAuditValue(object? value)
        {
            return value == null ? null : JsonSerializer.Serialize(value);
        }

        private static string FormatAuditValue(object? value)
        {
            if (value == null)
                return string.Empty;

            return value switch
            {
                string stringValue => stringValue.Trim(),
                bool boolValue => boolValue ? "Yes" : "No",
                DateTime dateTime => dateTime.ToUniversalTime().ToString("u"),
                decimal decimalValue => decimalValue.ToString("0.##", CultureInfo.InvariantCulture),
                int intValue => intValue.ToString(CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
            };
        }

        private static string NormalizeBorrowerType(string? borrowerType)
        {
            return string.IsNullOrWhiteSpace(borrowerType)
                ? "Borrower"
                : borrowerType.Trim();
        }

        private static int GetBorrowerSortOrder(string borrowerType)
        {
            if (borrowerType.Contains("Primary", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (borrowerType.Contains("Co", StringComparison.OrdinalIgnoreCase))
                return 1;

            return 2;
        }

        private static string GetBorrowerDisplayLabel(string borrowerType, int index)
        {
            var label = borrowerType switch
            {
                "PrimaryBorrower" => "Primary Borrower",
                "CoBorrower" => "Co-Borrower",
                "Co-Borrower" => "Co-Borrower",
                _ => borrowerType.Replace("Borrower", " Borrower", StringComparison.OrdinalIgnoreCase).Trim()
            };

            return index == 0 ? label : $"{label} #{index + 1}";
        }

        private sealed class AuditFieldChange
        {
            public AuditFieldChange(string label, string? oldValue, string? newValue)
            {
                Label = label;
                OldValue = oldValue;
                NewValue = newValue;
            }

            public string Label { get; }

            public string? OldValue { get; }

            public string? NewValue { get; }
        }

        private async Task SaveAggregateAsync(
            Loan loan,
            ManualMortgageApplicationRequest request,
            string companyNmlsNumber,
            CancellationToken cancellationToken,
            bool includeBorrowers = true)
        {
            var structure = request.LoanStructure ?? new ManualLoanStructureRequest();
            var housingInputs = request.HousingExpenseInputs;
            var title = request.TitleInfo ?? new ManualTitleInfoRequest();
            var company = await _context.Companies
                .FirstOrDefaultAsync(x => x.NmlsNumber == companyNmlsNumber, cancellationToken);

            if (company == null)
                throw new InvalidOperationException($"Company not found for NMLS {companyNmlsNumber}.");

            var subordinateLiens = request.SubordinateLiens ?? new List<ManualSubordinateLienRequest>();
            var subordinateLienTotal = subordinateLiens.Sum(x => x.LoanAmount ?? 0m);

            var loanTerms = new LoanTerms
            {
                CompanyNmlsNumber = companyNmlsNumber,
                LoanId = loan.Id,
                NoteAmount = request.Loan.NoteAmount,
                BaseLoanAmount = request.Loan.BaseLoanAmount ?? structure.BaseLoanAmount ?? request.Loan.NoteAmount,
                TotalLoanAmount = request.Loan.TotalLoanAmount ?? structure.TotalLoanAmount ?? request.Loan.NoteAmount,
                NoteRatePercent = request.Loan.NoteRatePercent,
                QualifyingRate = request.Loan.QualifyingRate ?? structure.QualifyingRate,
                LoanAmortizationPeriodCount = request.Loan.LoanAmortizationPeriodCount,
                LoanAmortizationType = request.Loan.LoanAmortizationType,
                InterestRateBuydown = request.Loan.InterestRateBuydown ?? structure.InterestRateBuydown,
                InterestOnly = request.Loan.InterestOnly ?? structure.InterestOnly,
                InterestOnlyTermMonths = request.Loan.InterestOnlyTermMonths ?? structure.InterestOnlyTermMonths,
                ImpoundWaiver = request.Loan.ImpoundWaiver ?? structure.ImpoundWaiver,
                LoanFico = request.Loan.LoanFico ?? structure.LoanFico,
                NoFico = request.Loan.NoFico ?? structure.NoFico,
                ProjectedReserveMonths = request.Loan.ProjectedReserveMonths ?? structure.ProjectedReserveMonths,
                Ltv = request.Loan.Ltv ?? structure.Ltv,
                Cltv = request.Loan.Cltv ?? structure.Cltv,
                Hcltv = request.Loan.Hcltv ?? structure.Hcltv,
                RefinanceType = request.Loan.RefinanceType ?? structure.RefinanceType,
                RefinanceProgram = request.Loan.RefinanceProgram ?? structure.RefinanceProgram,
                ExistingLiensAmount = request.Loan.ExistingLiensAmount ?? structure.ExistingLiensAmount ?? subordinateLienTotal,
                PurchasePrice = structure.PurchasePrice,
                DownPaymentAmount = structure.DownPaymentAmount,
                DownPaymentPercent = structure.DownPaymentPercent,
                LpaOffering = structure.LpaOffering,
                AlternateDoc = structure.AlternateDoc,
                TemporaryBuydown = structure.TemporaryBuydown,
                BusinessPurpose = structure.BusinessPurpose,
                BridgeLoan = structure.BridgeLoan,
                AssetDepletion = structure.AssetDepletion,
                InvestmentIncomeMode = structure.InvestmentIncomeMode,
                MonthlyGrossRentalIncome = structure.MonthlyGrossRentalIncome,
                RentalPercentage = structure.RentalPercentage,
                TotalCalculatedRent = structure.TotalCalculatedRent,
                TotalExpenses = structure.TotalExpenses,
                NetSubjectIncome = structure.NetSubjectIncome,
                LoanPurposeType = request.Loan.LoanPurposeType,
                MortgageType = request.Loan.MortgageType,
                LienPriorityType = request.Loan.LienPriorityType,
                CreatedAt = DateTime.UtcNow
            };
            _context.LoanTerms.Add(loanTerms);

            var subjectProperty = new SubjectProperty
            {
                CompanyNmlsNumber = companyNmlsNumber,
                LoanId = loan.Id,
                AddressLineText = request.SubjectProperty.AddressLineText,
                UnitIdentifier = request.SubjectProperty.UnitIdentifier,
                CityName = request.SubjectProperty.CityName,
                StateCode = request.SubjectProperty.StateCode,
                PostalCode = request.SubjectProperty.PostalCode,
                County = request.SubjectProperty.County,
                PropertyUsageType = request.SubjectProperty.PropertyUsageType,
                PropertyType = request.SubjectProperty.PropertyType,
                ConstructionMethod = request.SubjectProperty.ConstructionMethod,
                Occupancy = request.SubjectProperty.Occupancy,
                FinancedUnitCount = request.SubjectProperty.FinancedUnitCount,
                MixedUseProperty = request.SubjectProperty.MixedUseProperty,
                NonOccupantCoBorrower = request.SubjectProperty.NonOccupantCoBorrower,
                CommunityPropertyState = request.SubjectProperty.CommunityPropertyState,
                EnergyImprovement = request.SubjectProperty.EnergyImprovement,
                SolarLienPriority = request.SubjectProperty.SolarLienPriority,
                ConversionOfContract = request.SubjectProperty.ConversionOfContract,
                Renovation = request.SubjectProperty.Renovation,
                ConstructionLoan = request.SubjectProperty.ConstructionLoan,
                LotAcquiredDate = request.SubjectProperty.LotAcquiredDate,
                OriginalCostOfLot = request.SubjectProperty.OriginalCostOfLot,
                AttachmentType = request.SubjectProperty.AttachmentType,
                StructureType = request.SubjectProperty.StructureType,
                DesignType = request.SubjectProperty.DesignType,
                YearBuilt = request.SubjectProperty.YearBuilt,
                Acreage = request.SubjectProperty.Acreage,
                Improvements = request.SubjectProperty.Improvements,
                ImprovementCosts = request.SubjectProperty.ImprovementCosts,
                AccessoryDwellingUnitCount = request.SubjectProperty.AccessoryDwellingUnitCount,
                ConstructionStatus = request.SubjectProperty.ConstructionStatus,
                ProjectName = request.SubjectProperty.ProjectName,
                CondoProjectManagerId = request.SubjectProperty.CondoProjectManagerId,
                LegalDescription = request.SubjectProperty.LegalDescription,
                PropertyEstimatedValueAmount = request.SubjectProperty.PropertyEstimatedValueAmount ?? 0m,
                CreatedAt = DateTime.UtcNow
            };
            _context.SubjectProperties.Add(subjectProperty);

            _context.LoanTitleInfos.Add(new LoanTitleInfo
            {
                CompanyNmlsNumber = companyNmlsNumber,
                LoanId = loan.Id,
                MannerTitleHeld = title.MannerTitleHeld,
                TitleHeldInNameOf = title.TitleHeldInNameOf,
                VestingToRead = title.VestingToRead,
                TrustInformation = title.TrustInformation,
                IndianCountryLandTenure = title.IndianCountryLandTenure,
                EstateHeldIn = title.EstateHeldIn,
                TitleInsuranceCompany = title.TitleInsuranceCompany,
                TitleCommitmentNumber = title.TitleCommitmentNumber,
                TitlePolicyNumber = title.TitlePolicyNumber,
                VestingEntityType = title.VestingEntityType,
                TitleNotes = title.TitleNotes,
                Homestead = title.Homestead ?? false,
                TitleCurative = title.TitleCurative ?? false,
                PowerOfAttorney = title.PowerOfAttorney ?? false,
                RecordedEasement = title.RecordedEasement ?? false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);

            var housingRows = BuildCanonicalHousingRows(request.HousingExpenses, structure, housingInputs);
            foreach (var housingRow in housingRows)
            {
                _context.HousingExpenses.Add(new HousingExpense
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    LoanId = loan.Id,
                    HousingExpenseType = housingRow.HousingExpenseType,
                    Mode = housingRow.Mode,
                    Factor = housingRow.Factor,
                    ValueAmount = housingRow.ValueAmount,
                    SourceType = housingRow.SourceType,
                    Description = housingRow.Description,
                    InitialAnnualRate = housingRow.InitialAnnualRate,
                    InitialMonthlyAmount = housingRow.InitialMonthlyAmount,
                    RenewalAnnualRate = housingRow.RenewalAnnualRate,
                    RenewalMonthlyAmount = housingRow.RenewalMonthlyAmount,
                    Included = housingRow.Included,
                    DisplayOrder = housingRow.DisplayOrder,
                    PresentHousingExpenseAmount = housingRow.PresentHousingExpenseAmount,
                    ProposedHousingExpenseAmount = housingRow.ProposedHousingExpenseAmount,
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var source in request.DownPaymentSources)
            {
                if (!source.Amount.HasValue || source.Amount.Value <= 0m)
                    continue;

                if (string.IsNullOrWhiteSpace(source.SourceType) || string.Equals(source.SourceType, "Select", StringComparison.OrdinalIgnoreCase))
                    continue;

                _context.LoanDownPaymentSources.Add(new LoanDownPaymentSource
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    LoanId = loan.Id,
                    SourceType = source.SourceType,
                    Amount = source.Amount.Value,
                    Included = source.Included,
                    DisplayOrder = source.DisplayOrder,
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var credit in request.PurchaseCredits)
            {
                if (!credit.Amount.HasValue || credit.Amount.Value <= 0m)
                    continue;

                if (string.IsNullOrWhiteSpace(credit.CreditType) || string.Equals(credit.CreditType, "- Select -", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrWhiteSpace(credit.SourceType) || string.Equals(credit.SourceType, "- Select -", StringComparison.OrdinalIgnoreCase))
                    continue;

                _context.LoanPurchaseCredits.Add(new LoanPurchaseCredit
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    LoanId = loan.Id,
                    CreditType = credit.CreditType,
                    SourceType = credit.SourceType,
                    Amount = credit.Amount.Value,
                    DisplayOrder = credit.DisplayOrder,
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var subordinateLien in subordinateLiens)
            {
                if (string.IsNullOrWhiteSpace(subordinateLien.LienType))
                    continue;

                if (!subordinateLien.LoanAmount.HasValue && !subordinateLien.MonthlyPaymentAmount.HasValue && string.IsNullOrWhiteSpace(subordinateLien.LienName) && string.IsNullOrWhiteSpace(subordinateLien.SourceType))
                    continue;

                var subordinateLienEntity = new LoanSubordinateLien
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    LoanId = loan.Id,
                    LienType = subordinateLien.LienType,
                    LienName = subordinateLien.LienName,
                    MonthlyPaymentAmount = subordinateLien.MonthlyPaymentAmount,
                    LoanAmount = subordinateLien.LoanAmount,
                    SourceType = subordinateLien.SourceType,
                    DisplayOrder = subordinateLien.DisplayOrder,
                    CreatedAt = DateTime.UtcNow
                };

                _context.LoanSubordinateLiens.Add(subordinateLienEntity);
                _context.Entry(subordinateLienEntity).Property("CompanyId").CurrentValue = company.Id;
            }

            var borrowerApplicationAssignments = BuildBorrowerApplicationAssignments(request.BorrowerApplications);

            if (includeBorrowers)
                await SyncBorrowersAsync(loan, request.Borrowers, borrowerApplicationAssignments, companyNmlsNumber, loan.UserId, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task SyncBorrowersAsync(
            Loan loan,
            IReadOnlyList<ManualBorrowerRequest> borrowerRequests,
            IReadOnlyDictionary<int, (int ApplicationNumber, int BorrowerOrder)> borrowerApplicationAssignments,
            string companyNmlsNumber,
            int? changedByUserId,
            CancellationToken cancellationToken)
        {
            var existingBorrowers = await _context.Borrowers
                .Where(x => x.LoanId == loan.Id && x.CompanyNmlsNumber == companyNmlsNumber)
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken);

            var existingById = existingBorrowers.ToDictionary(x => x.Id);
            var remainingBorrowerIds = existingBorrowers.Select(x => x.Id).ToHashSet();

            foreach (var borrowerRequest in borrowerRequests)
            {
                var canonicalBorrowerType = CanonicalizeBorrowerType(borrowerRequest.BorrowerType);
                Borrower? borrower = null;

                if (borrowerRequest.Id.HasValue && existingById.TryGetValue(borrowerRequest.Id.Value, out var borrowerById) && remainingBorrowerIds.Contains(borrowerById.Id))
                    borrower = borrowerById;

                if (borrower == null)
                {
                    borrower = new Borrower
                    {
                        CompanyNmlsNumber = companyNmlsNumber,
                        LoanId = loan.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Borrowers.Add(borrower);
                }

                borrower.BorrowerType = canonicalBorrowerType;
                borrower.FirstName = borrowerRequest.FirstName;
                borrower.MiddleName = borrowerRequest.MiddleName;
                borrower.LastName = borrowerRequest.LastName;
                borrower.Suffix = borrowerRequest.Suffix;
                borrower.Nickname = borrowerRequest.Nickname;
                borrower.SsnItin = borrowerRequest.SsnItin;
                borrower.Email = borrowerRequest.Email;
                borrower.CellPhone = borrowerRequest.CellPhone;
                borrower.HomePhone = borrowerRequest.HomePhone;
                borrower.WorkPhone = borrowerRequest.WorkPhone;
                borrower.WorkPhoneExtension = borrowerRequest.WorkPhoneExtension;
                borrower.MaritalStatus = borrowerRequest.MaritalStatus;
                borrower.DateOfBirth = ParseDateOfBirth(borrowerRequest.DateOfBirth);
                borrower.EstimatedCreditScore = borrowerRequest.EstimatedCreditScore;
                borrower.EConsentAuthorized = borrowerRequest.EConsentAuthorized;
                borrower.CreditPullAuthorized = borrowerRequest.CreditPullAuthorized;
                borrower.NumberOfDependents = borrowerRequest.NumberOfDependents;
                borrower.DependentAges = borrowerRequest.DependentAges;
                borrower.MailingAddressSameAsCurrent = ResolveMailingAddressSameAsCurrent(borrowerRequest);
                borrower.OtherLanguageDescription = borrowerRequest.OtherLanguageDescription;
                borrower.LanguagePreferences = SerializeLanguagePreferences(borrowerRequest.LanguagePreferences);

                if (borrowerApplicationAssignments.TryGetValue(borrower.Id, out var assignment))
                {
                    borrower.ApplicationNumber = assignment.ApplicationNumber;
                    borrower.ApplicationBorrowerOrder = assignment.BorrowerOrder;
                }
                else
                {
                    // Fallback for clients that submit grouping directly on borrower rows.
                    if (borrowerRequest.ApplicationNumber is > 0)
                        borrower.ApplicationNumber = borrowerRequest.ApplicationNumber;

                    if (borrowerRequest.ApplicationBorrowerOrder is > 0)
                        borrower.ApplicationBorrowerOrder = borrowerRequest.ApplicationBorrowerOrder;
                }

                await _context.SaveChangesAsync(cancellationToken);

                await _contactService.FindOrCreateContactFromBorrowerAsync(
                    borrower,
                    changedByUserId,
                    cancellationToken);

                await SyncBorrowerChildrenAsync(
                    loan.Id,
                    borrower.Id,
                    borrowerRequest,
                    companyNmlsNumber,
                    cancellationToken);

                remainingBorrowerIds.Remove(borrower.Id);
            }

            if (remainingBorrowerIds.Count > 0)
            {
                var staleBorrowers = existingBorrowers
                    .Where(x => remainingBorrowerIds.Contains(x.Id))
                    .ToList();

                _context.Borrowers.RemoveRange(staleBorrowers);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task SyncBorrowerChildrenAsync(
            int loanId,
            int borrowerId,
            ManualBorrowerRequest borrowerRequest,
            string companyNmlsNumber,
            CancellationToken cancellationToken)
        {
            var existingAddresses = await _context.BorrowerAddresses
                .Where(x => x.BorrowerId == borrowerId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.BorrowerAddresses.RemoveRange(existingAddresses);

            var existingIncomes = await _context.BorrowerIncomes
                .Where(x => x.BorrowerId == borrowerId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.BorrowerIncomes.RemoveRange(existingIncomes);

            var existingAssets = await _context.BorrowerAssets
                .Where(x => x.BorrowerId == borrowerId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.BorrowerAssets.RemoveRange(existingAssets);

            var existingLiabilities = await _context.BorrowerLiabilities
                .Where(x => x.BorrowerId == borrowerId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.BorrowerLiabilities.RemoveRange(existingLiabilities);

            var existingDeclarations = await _context.BorrowerDeclarations
                .Where(x => x.LoanId == loanId && x.BorrowerId == borrowerId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.BorrowerDeclarations.RemoveRange(existingDeclarations);

            var existingGovernmentMonitoring = await _context.GovernmentMonitorings
                .Where(x => x.BorrowerId == borrowerId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.GovernmentMonitorings.RemoveRange(existingGovernmentMonitoring);

            var existingMilitaryInfo = await _context.BorrowerMilitaryInfos
                .Where(x => x.LoanId == loanId && x.BorrowerId == borrowerId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.BorrowerMilitaryInfos.RemoveRange(existingMilitaryInfo);

            var existingEmployments = await _context.BorrowerEmployments
                .Where(x => x.BorrowerId == borrowerId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.BorrowerEmployments.RemoveRange(existingEmployments);

            await _context.SaveChangesAsync(cancellationToken);

            foreach (var addressRequest in borrowerRequest.Addresses)
            {
                if (string.IsNullOrWhiteSpace(addressRequest.StreetAddress))
                    continue;

                _context.BorrowerAddresses.Add(new BorrowerAddress
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    BorrowerId = borrowerId,
                    AddressType = addressRequest.AddressType,
                    StreetAddress = addressRequest.StreetAddress,
                    City = addressRequest.City,
                    State = addressRequest.State,
                    ZipCode = addressRequest.ZipCode,
                    OccupancyType = addressRequest.OccupancyType,
                    YearsSpent = addressRequest.YearsSpent,
                    MonthsSpent = addressRequest.MonthsSpent
                });
            }

            int? firstEmploymentId = null;
            foreach (var employmentRequest in borrowerRequest.Employments)
            {
                if (string.IsNullOrWhiteSpace(employmentRequest.EmployerName))
                    continue;

                var employment = new BorrowerEmployment
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    BorrowerId = borrowerId,
                    EmployerName = employmentRequest.EmployerName,
                    EmploymentStatusType = employmentRequest.EmploymentStatusType,
                    EmploymentStartDate = employmentRequest.EmploymentStartDate,
                    EmploymentPositionDescription = employmentRequest.EmploymentPositionDescription,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BorrowerEmployments.Add(employment);
                await _context.SaveChangesAsync(cancellationToken);
                firstEmploymentId ??= employment.Id;
            }

            foreach (var incomeRequest in borrowerRequest.Incomes)
            {
                _context.BorrowerIncomes.Add(new BorrowerIncome
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    BorrowerId = borrowerId,
                    BorrowerEmploymentId = firstEmploymentId,
                    IncomeType = incomeRequest.IncomeType,
                    CurrentIncomeMonthlyTotalAmount = incomeRequest.CurrentIncomeMonthlyTotalAmount,
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var assetRequest in borrowerRequest.Assets)
            {
                _context.BorrowerAssets.Add(new BorrowerAsset
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    BorrowerId = borrowerId,
                    AssetType = assetRequest.AssetType,
                    AssetCashOrMarketValueAmount = assetRequest.AssetCashOrMarketValueAmount,
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var liabilityRequest in borrowerRequest.Liabilities)
            {
                _context.BorrowerLiabilities.Add(new BorrowerLiability
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    BorrowerId = borrowerId,
                    LiabilityType = liabilityRequest.LiabilityType,
                    MonthlyPaymentAmount = liabilityRequest.MonthlyPaymentAmount ?? 0m,
                    UPBAmount = liabilityRequest.UPBAmount ?? 0m,
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var declarationRequest in borrowerRequest.Declarations)
            {
                _context.BorrowerDeclarations.Add(new BorrowerDeclaration
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    LoanId = loanId,
                    BorrowerId = borrowerId,
                    DeclarationType = declarationRequest.DeclarationType,
                    IsAffirmative = declarationRequest.IsAffirmative,
                    Explanation = declarationRequest.Explanation,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (borrowerRequest.GovernmentMonitoring != null)
            {
                _context.GovernmentMonitorings.Add(new GovernmentMonitoring
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    BorrowerId = borrowerId,
                    ToBeCompletedByFinancialInstitution = borrowerRequest.GovernmentMonitoring.ToBeCompletedByFinancialInstitution,
                    EthnicityType = borrowerRequest.GovernmentMonitoring.EthnicityType,
                    EthnicityOriginOther = borrowerRequest.GovernmentMonitoring.EthnicityOriginOther,
                    RaceType = borrowerRequest.GovernmentMonitoring.RaceType,
                    RaceOther = borrowerRequest.GovernmentMonitoring.RaceOther,
                    SexType = borrowerRequest.GovernmentMonitoring.SexType,
                    CollectionMethodType = borrowerRequest.GovernmentMonitoring.CollectionMethodType,
                    CollectedByVisualObservation = borrowerRequest.GovernmentMonitoring.CollectedByVisualObservation,
                    EthnicityVisualObservationIndicator = borrowerRequest.GovernmentMonitoring.CollectedEthnicityByObservation ?? borrowerRequest.GovernmentMonitoring.EthnicityVisualObservationIndicator,
                    RaceVisualObservationIndicator = borrowerRequest.GovernmentMonitoring.CollectedRaceByObservation ?? borrowerRequest.GovernmentMonitoring.RaceVisualObservationIndicator,
                    SexVisualObservationIndicator = borrowerRequest.GovernmentMonitoring.CollectedSexByObservation ?? borrowerRequest.GovernmentMonitoring.SexVisualObservationIndicator,
                    HmdaMonitoringType = borrowerRequest.GovernmentMonitoring.HmdaMonitoringType,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (borrowerRequest.MilitaryInformation != null && HasMilitaryInformationValues(borrowerRequest.MilitaryInformation))
            {
                _context.BorrowerMilitaryInfos.Add(new BorrowerMilitaryInfo
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    LoanId = loanId,
                    BorrowerId = borrowerId,
                    MilitaryStatus = borrowerRequest.MilitaryInformation.MilitaryStatus,
                    ActiveDuty = borrowerRequest.MilitaryInformation.ActiveDuty,
                    Veteran = borrowerRequest.MilitaryInformation.Veteran,
                    Reserves = borrowerRequest.MilitaryInformation.Reserves,
                    NationalGuard = borrowerRequest.MilitaryInformation.NationalGuard,
                    ServiceBranch = borrowerRequest.MilitaryInformation.ServiceBranch,
                    ServiceStartDate = NormalizeUtcDate(borrowerRequest.MilitaryInformation.ServiceStartDate),
                    ServiceEndDate = NormalizeUtcDate(borrowerRequest.MilitaryInformation.ServiceEndDate),
                    ProjectedExpirationDate = NormalizeUtcDate(borrowerRequest.MilitaryInformation.ProjectedExpirationDate),
                    SurvivorSpouse = borrowerRequest.MilitaryInformation.SurvivorSpouse,
                    VaEligible = borrowerRequest.MilitaryInformation.VaEligible,
                    VaEligibilityStatus = borrowerRequest.MilitaryInformation.VaEligibilityStatus,
                    VaFirstTimeUse = borrowerRequest.MilitaryInformation.VaFirstTimeUse,
                    VaFundingFeeExempt = borrowerRequest.MilitaryInformation.VaFundingFeeExempt,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static string NormalizeBorrowerTypeKey(string? borrowerType)
        {
            if (string.IsNullOrWhiteSpace(borrowerType))
                return "borrower";

            return borrowerType
                .Trim()
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();
        }

        private static string CanonicalizeBorrowerType(string? borrowerType)
        {
            var key = NormalizeBorrowerTypeKey(borrowerType);

            if (key.Contains("primary", StringComparison.OrdinalIgnoreCase))
                return "PrimaryBorrower";

            if (key.Contains("co", StringComparison.OrdinalIgnoreCase))
                return "CoBorrower";

            return string.IsNullOrWhiteSpace(borrowerType)
                ? "Borrower"
                : borrowerType.Trim();
        }

        private static bool HasMilitaryInformationValues(ManualBorrowerMilitaryInfoRequest militaryInformation)
        {
            return !string.IsNullOrWhiteSpace(militaryInformation.MilitaryStatus)
                || militaryInformation.ActiveDuty.HasValue
                || militaryInformation.Veteran.HasValue
                || militaryInformation.Reserves.HasValue
                || militaryInformation.NationalGuard.HasValue
                || !string.IsNullOrWhiteSpace(militaryInformation.ServiceBranch)
                || militaryInformation.ServiceStartDate.HasValue
                || militaryInformation.ServiceEndDate.HasValue
                || militaryInformation.ProjectedExpirationDate.HasValue
                || militaryInformation.SurvivorSpouse.HasValue
                || militaryInformation.VaEligible.HasValue
                || !string.IsNullOrWhiteSpace(militaryInformation.VaEligibilityStatus)
                || militaryInformation.VaFirstTimeUse.HasValue
                || militaryInformation.VaFundingFeeExempt.HasValue;
        }

        private static DateTime? NormalizeUtcDate(DateTime? dateValue)
        {
            if (!dateValue.HasValue)
                return null;

            return dateValue.Value.Kind switch
            {
                DateTimeKind.Utc => dateValue.Value,
                DateTimeKind.Local => dateValue.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dateValue.Value, DateTimeKind.Utc)
            };
        }

        private static string NormalizeHousingExpenseType(string value)
        {
            var normalized = value.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);

            return normalized.ToLowerInvariant() switch
            {
                "otherfinancing" => "OtherFinancing",
                "hoi" => "HOI",
                "supplemental" => "Supplemental",
                "propertytaxes" => "PropertyTaxes",
                "mi" => "MI",
                "associationdues" => "AssociationDues",
                "firstmortgage" => "FirstMortgage",
                "other" => "Other",
                _ => value
            };
        }

        private async Task ClearAggregateAsync(int loanId, string companyNmlsNumber, CancellationToken cancellationToken, bool includeBorrowers = true)
        {
            var housingExpenses = await _context.HousingExpenses
                .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.HousingExpenses.RemoveRange(housingExpenses);

            var titleInfo = await _context.LoanTitleInfos
                .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.LoanTitleInfos.RemoveRange(titleInfo);

            var downPaymentSources = await _context.LoanDownPaymentSources
                .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.LoanDownPaymentSources.RemoveRange(downPaymentSources);

            var purchaseCredits = await _context.LoanPurchaseCredits
                .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.LoanPurchaseCredits.RemoveRange(purchaseCredits);

            var subordinateLiens = await _context.LoanSubordinateLiens
                .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.LoanSubordinateLiens.RemoveRange(subordinateLiens);

            if (includeBorrowers)
            {
                var borrowerIds = await _context.Borrowers
                    .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                var declarations = await _context.BorrowerDeclarations
                    .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                    .ToListAsync(cancellationToken);
                _context.BorrowerDeclarations.RemoveRange(declarations);

                var monitorings = await _context.GovernmentMonitorings
                    .Where(x => borrowerIds.Contains(x.BorrowerId) && x.CompanyNmlsNumber == companyNmlsNumber)
                    .ToListAsync(cancellationToken);
                _context.GovernmentMonitorings.RemoveRange(monitorings);
            }

            var loanTerms = await _context.LoanTerms
                .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.LoanTerms.RemoveRange(loanTerms);

            var properties = await _context.SubjectProperties
                .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.SubjectProperties.RemoveRange(properties);

            if (includeBorrowers)
            {
                var borrowers = await _context.Borrowers
                    .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                    .ToListAsync(cancellationToken);
                _context.Borrowers.RemoveRange(borrowers);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static List<HousingExpenseWriteModel> BuildCanonicalHousingRows(
            List<ManualHousingExpenseRequest> housingExpenses,
            ManualLoanStructureRequest structure,
            ManualHousingExpenseInputsRequest? housingInputs)
        {
            var rows = new Dictionary<string, HousingExpenseWriteModel>(StringComparer.OrdinalIgnoreCase);

            foreach (var housingExpenseRequest in housingExpenses)
            {
                var housingExpenseType = string.IsNullOrWhiteSpace(housingExpenseRequest.HousingExpenseType)
                    ? housingExpenseRequest.ExpenseType
                    : housingExpenseRequest.HousingExpenseType;

                if (string.IsNullOrWhiteSpace(housingExpenseType))
                    continue;

                var canonicalHousingExpenseType = NormalizeHousingExpenseType(housingExpenseType);
                var resolvedAmount = housingExpenseRequest.Amount ?? housingExpenseRequest.ProposedHousingExpenseAmount;
                var resolvedMode = housingExpenseRequest.Mode;
                var resolvedSourceType = housingExpenseRequest.SourceType;
                var resolvedDescription = housingExpenseRequest.Description;
                var resolvedInitialAnnualRate = housingExpenseRequest.InitialAnnualRate;
                var resolvedInitialMonthlyAmount = housingExpenseRequest.InitialMonthlyAmount;
                var resolvedRenewalAnnualRate = housingExpenseRequest.RenewalAnnualRate;
                var resolvedRenewalMonthlyAmount = housingExpenseRequest.RenewalMonthlyAmount;
                var resolvedIsCalc = housingExpenseRequest.Included;

                if (canonicalHousingExpenseType == "OtherFinancing")
                {
                    if (!string.IsNullOrWhiteSpace(housingInputs?.OtherFinancing?.Factor))
                        resolvedMode = housingInputs.OtherFinancing.Factor;
                    resolvedIsCalc = housingInputs?.OtherFinancing?.IsCalc ?? resolvedIsCalc;
                }

                if (canonicalHousingExpenseType == "MI" && !string.IsNullOrWhiteSpace(housingInputs?.Mi?.Mode))
                    resolvedMode = housingInputs.Mi.Mode;

                if (canonicalHousingExpenseType == "MI")
                {
                    resolvedInitialAnnualRate ??= ParseNullableDecimal(housingInputs?.Mi?.MortgageInsurance?.InitialAnnualRate);
                    resolvedInitialMonthlyAmount ??= ParseNullableDecimal(housingInputs?.Mi?.MortgageInsurance?.InitialMonthly);
                    resolvedRenewalAnnualRate ??= ParseNullableDecimal(housingInputs?.Mi?.MortgageInsurance?.RenewalAnnualRate);
                    resolvedRenewalMonthlyAmount ??= ParseNullableDecimal(housingInputs?.Mi?.MortgageInsurance?.RenewalMonthly);

                    var miValueAmount = ParseNullableDecimal(housingInputs?.Mi?.Value);
                    if (!resolvedInitialMonthlyAmount.HasValue && miValueAmount.HasValue)
                        resolvedInitialMonthlyAmount = miValueAmount;
                }

                if (canonicalHousingExpenseType == "HOI")
                {
                    if (!string.IsNullOrWhiteSpace(housingInputs?.Hoi?.Factor))
                        resolvedMode = housingInputs.Hoi.Factor;
                    resolvedSourceType ??= housingInputs?.Hoi?.Source;
                    resolvedIsCalc = housingInputs?.Hoi?.IsCalc ?? resolvedIsCalc;
                }

                if (canonicalHousingExpenseType == "Supplemental")
                {
                    if (!string.IsNullOrWhiteSpace(housingInputs?.Supplemental?.Factor))
                        resolvedMode = housingInputs.Supplemental.Factor;
                    resolvedSourceType ??= housingInputs?.Supplemental?.Source;
                    resolvedIsCalc = housingInputs?.Supplemental?.IsCalc ?? resolvedIsCalc;
                }

                if (canonicalHousingExpenseType == "PropertyTaxes")
                {
                    if (!string.IsNullOrWhiteSpace(housingInputs?.PropertyTaxes?.Factor))
                        resolvedMode = housingInputs.PropertyTaxes.Factor;
                    resolvedSourceType ??= housingInputs?.PropertyTaxes?.Source;
                    resolvedIsCalc = housingInputs?.PropertyTaxes?.IsCalc ?? resolvedIsCalc;
                }

                if (canonicalHousingExpenseType == "Other" && string.IsNullOrWhiteSpace(resolvedDescription))
                    resolvedDescription = housingInputs?.Other?.Description;

                rows[canonicalHousingExpenseType] = new HousingExpenseWriteModel
                {
                    HousingExpenseType = canonicalHousingExpenseType,
                    Mode = resolvedMode,
                    Factor = housingExpenseRequest.Factor,
                    ValueAmount = housingExpenseRequest.ValueAmount,
                    SourceType = resolvedSourceType,
                    Description = resolvedDescription,
                    InitialAnnualRate = resolvedInitialAnnualRate,
                    InitialMonthlyAmount = resolvedInitialMonthlyAmount,
                    RenewalAnnualRate = resolvedRenewalAnnualRate,
                    RenewalMonthlyAmount = resolvedRenewalMonthlyAmount,
                    Included = resolvedIsCalc,
                    DisplayOrder = housingExpenseRequest.DisplayOrder ?? 0,
                    PresentHousingExpenseAmount = housingExpenseRequest.PresentHousingExpenseAmount == 0m
                        ? resolvedAmount
                        : housingExpenseRequest.PresentHousingExpenseAmount,
                    ProposedHousingExpenseAmount = resolvedAmount
                };
            }

            AddStructureHousingFallback(rows, "FirstMortgage", structure.FirstMortgage, null, null, null, null, null, null, null, null, null, true, 100);
            AddStructureHousingFallback(rows, "OtherFinancing", structure.OtherFinancing, structure.OtherFinancingMode, structure.OtherFinancingFactor, structure.OtherFinancingValue, null, null, null, null, null, null, true, 110);
            AddStructureHousingFallback(rows, "HOI", structure.Hoi, structure.HoiMode, structure.HoiFactor, null, structure.HoiSource, null, null, null, null, null, true, 120);
            AddStructureHousingFallback(rows, "Supplemental", structure.Supplemental, structure.SupplementalMode, structure.SupplementalFactor, null, structure.SupplementalSource, null, null, null, null, null, true, 130);
            AddStructureHousingFallback(rows, "PropertyTaxes", structure.PropertyTaxes, structure.PropertyTaxesMode, structure.PropertyTaxesFactor, null, structure.PropertyTaxesSource, null, null, null, null, null, true, 140);
            AddStructureHousingFallback(rows, "MI", null, structure.MiMode, null, null, null, null, structure.MiInitialAnnualRate, structure.MiInitialMonthly, structure.MiRenewalAnnualRate, structure.MiRenewalMonthly, true, 150);
            AddStructureHousingFallback(rows, "AssociationDues", structure.AssociationDues, null, null, null, null, null, null, null, null, null, true, 160);
            AddStructureHousingFallback(rows, "Other", structure.OtherAmount, null, null, null, null, structure.OtherDescription, null, null, null, null, true, 170);
            AddStructureHousingFallback(rows, "TotalPITI", structure.TotalPiti, null, null, null, null, null, null, null, null, null, true, 180);

            return rows.Values.OrderBy(x => x.DisplayOrder).ThenBy(x => x.HousingExpenseType).ToList();
        }

        private static decimal? ParseNullableDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        private static void AddStructureHousingFallback(
            Dictionary<string, HousingExpenseWriteModel> rows,
            string type,
            decimal? proposedAmount,
            string? mode,
            decimal? factor,
            decimal? value,
            string? source,
            string? description,
            decimal? initialAnnualRate,
            decimal? initialMonthly,
            decimal? renewalAnnualRate,
            decimal? renewalMonthly,
            bool? included,
            int displayOrder)
        {
            if (rows.ContainsKey(type))
                return;

            if (!proposedAmount.HasValue &&
                string.IsNullOrWhiteSpace(mode) &&
                !factor.HasValue &&
                !value.HasValue &&
                string.IsNullOrWhiteSpace(source) &&
                string.IsNullOrWhiteSpace(description) &&
                !initialAnnualRate.HasValue &&
                !initialMonthly.HasValue &&
                !renewalAnnualRate.HasValue &&
                !renewalMonthly.HasValue)
            {
                return;
            }

            rows[type] = new HousingExpenseWriteModel
            {
                HousingExpenseType = type,
                Mode = mode,
                Factor = factor,
                ValueAmount = value,
                SourceType = source,
                Description = description,
                InitialAnnualRate = initialAnnualRate,
                InitialMonthlyAmount = initialMonthly,
                RenewalAnnualRate = renewalAnnualRate,
                RenewalMonthlyAmount = renewalMonthly,
                Included = included,
                DisplayOrder = displayOrder,
                PresentHousingExpenseAmount = 0m,
                ProposedHousingExpenseAmount = proposedAmount ?? 0m
            };
        }

        private sealed class HousingExpenseWriteModel
        {
            public string HousingExpenseType { get; set; } = string.Empty;
            public string? Mode { get; set; }
            public decimal? Factor { get; set; }
            public decimal? ValueAmount { get; set; }
            public string? SourceType { get; set; }
            public string? Description { get; set; }
            public decimal? InitialAnnualRate { get; set; }
            public decimal? InitialMonthlyAmount { get; set; }
            public decimal? RenewalAnnualRate { get; set; }
            public decimal? RenewalMonthlyAmount { get; set; }
            public bool? Included { get; set; }
            public int DisplayOrder { get; set; }
            public decimal PresentHousingExpenseAmount { get; set; }
            public decimal ProposedHousingExpenseAmount { get; set; }
        }

        private async Task<ManualMortgageApplicationResponse?> BuildApplicationResponseAsync(
            string loanNumber,
            string companyNmlsNumber,
            CancellationToken cancellationToken)
        {
            var loan = await _context.Loans
                .FirstOrDefaultAsync(x => x.LoanNumber == loanNumber && x.CompanyNmlsNumber == companyNmlsNumber, cancellationToken);

            if (loan == null)
                return null;

            var loanTerms = await _context.LoanTerms
                .FirstOrDefaultAsync(x => x.LoanId == loan.Id && x.CompanyNmlsNumber == companyNmlsNumber, cancellationToken);

            var subjectProperty = await _context.SubjectProperties
                .FirstOrDefaultAsync(x => x.LoanId == loan.Id && x.CompanyNmlsNumber == companyNmlsNumber, cancellationToken);

            var housingExpenses = await _context.HousingExpenses
                .Where(x => x.LoanId == loan.Id && x.CompanyNmlsNumber == companyNmlsNumber)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(cancellationToken);

            var titleInfo = await _context.LoanTitleInfos
                .FirstOrDefaultAsync(x => x.LoanId == loan.Id && x.CompanyNmlsNumber == companyNmlsNumber, cancellationToken);

            var downPaymentSources = await _context.LoanDownPaymentSources
                .Where(x => x.LoanId == loan.Id && x.CompanyNmlsNumber == companyNmlsNumber)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(cancellationToken);

            var purchaseCredits = await _context.LoanPurchaseCredits
                .Where(x => x.LoanId == loan.Id && x.CompanyNmlsNumber == companyNmlsNumber)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(cancellationToken);

            var subordinateLiens = await _context.LoanSubordinateLiens
                .Where(x => x.LoanId == loan.Id && x.CompanyNmlsNumber == companyNmlsNumber)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);

            var borrowers = await _context.Borrowers
                .Where(x => x.LoanId == loan.Id && x.CompanyNmlsNumber == companyNmlsNumber)
                .OrderBy(x => x.ApplicationNumber ?? int.MaxValue)
                .ThenBy(x => x.ApplicationBorrowerOrder ?? int.MaxValue)
                .ToListAsync(cancellationToken);

            var borrowerIds = borrowers.Select(x => x.Id).ToList();

            var addresses = await _context.BorrowerAddresses
                .Where(x => borrowerIds.Contains(x.BorrowerId) && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);

            var employments = await _context.BorrowerEmployments
                .Where(x => borrowerIds.Contains(x.BorrowerId) && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);

            var incomes = await _context.BorrowerIncomes
                .Where(x => borrowerIds.Contains(x.BorrowerId) && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);

            var assets = await _context.BorrowerAssets
                .Where(x => borrowerIds.Contains(x.BorrowerId) && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);

            var liabilities = await _context.BorrowerLiabilities
                .Where(x => borrowerIds.Contains(x.BorrowerId) && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);

            var declarations = await _context.BorrowerDeclarations
                .Where(x => x.LoanId == loan.Id && borrowerIds.Contains(x.BorrowerId) && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);

            var governmentMonitorings = await _context.GovernmentMonitorings
                .Where(x => borrowerIds.Contains(x.BorrowerId) && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);

            var militaryInfos = await _context.BorrowerMilitaryInfos
                .Where(x => x.LoanId == loan.Id && borrowerIds.Contains(x.BorrowerId) && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);

            var lenderName = await _context.Companies
                .Where(x => x.NmlsNumber == companyNmlsNumber)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);

            var primaryBorrower = borrowers
                .OrderByDescending(x => string.Equals(x.BorrowerType, "PrimaryBorrower", StringComparison.OrdinalIgnoreCase))
                .ThenBy(x => x.Id)
                .FirstOrDefault();

            var fico = primaryBorrower?.EstimatedCreditScore
                ?? borrowers.Select(x => x.EstimatedCreditScore).FirstOrDefault(x => x.HasValue);

            var titleAgent = await _context.LoanContacts
                .Where(x =>
                    x.LoanId == loan.Id &&
                    x.CompanyNmlsNumber == companyNmlsNumber &&
                    x.Role == LoanContactRole.TitleAgent &&
                    x.IsActive)
                .Join(
                    _context.Contacts,
                    lc => lc.ContactId,
                    c => c.Id,
                    (lc, c) => new
                    {
                        lc.IsPrimary,
                        c.FullName,
                        c.Email,
                        c.WorkPhone,
                        c.MobilePhone,
                        c.IsActive
                    })
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.IsPrimary)
                .FirstOrDefaultAsync(cancellationToken);

            var latestAusRun = await _context.AUSRuns
                .Where(x => x.LoanId == loan.Id && x.CompanyNmlsNumber == companyNmlsNumber)
                .OrderByDescending(x => x.RequestedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            AUSFinding? duFinding = null;
            AUSFinding? lpaFinding = null;

            if (latestAusRun != null)
            {
                var findings = await _context.AUSFindings
                    .Where(x => x.AUSProviderRun.AUSRunId == latestAusRun.Id)
                    .ToListAsync(cancellationToken);

                duFinding = findings.FirstOrDefault(x => x.Provider == AUSProvider.FannieMaeDU);
                lpaFinding = findings.FirstOrDefault(x => x.Provider == AUSProvider.FreddieMacLPA);
            }

            var propertyAddressSummary = string.Join(", ", new[]
            {
                loan.Subject_Street_Address,
                loan.Subject_City,
                loan.Subject_State,
                loan.Subject_ZipCode
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            var miHousingRow = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "MI");

            return new ManualMortgageApplicationResponse
            {
                LoanNumber = loan.LoanNumber,
                Loan = new ManualLoanSnapshotResponse
                {
                    LoanPurposeType = loanTerms?.LoanPurposeType,
                    MortgageType = loanTerms?.MortgageType,
                    LienPriorityType = loanTerms?.LienPriorityType,
                    NoteAmount = loanTerms?.NoteAmount ?? loan.LoanAmount,
                    BaseLoanAmount = loanTerms?.BaseLoanAmount,
                    TotalLoanAmount = loanTerms?.TotalLoanAmount,
                    NoteRatePercent = loanTerms?.NoteRatePercent,
                    QualifyingRate = loanTerms?.QualifyingRate,
                    LoanAmortizationPeriodCount = loanTerms?.LoanAmortizationPeriodCount,
                    LoanAmortizationType = loanTerms?.LoanAmortizationType,
                    InterestRateBuydown = loanTerms?.InterestRateBuydown,
                    InterestOnly = loanTerms?.InterestOnly,
                    InterestOnlyTermMonths = loanTerms?.InterestOnlyTermMonths,
                    ImpoundWaiver = loanTerms?.ImpoundWaiver,
                    LoanFico = loanTerms?.LoanFico,
                    NoFico = loanTerms?.NoFico,
                    ProjectedReserveMonths = loanTerms?.ProjectedReserveMonths,
                    Ltv = loanTerms?.Ltv,
                    Cltv = loanTerms?.Cltv,
                    Hcltv = loanTerms?.Hcltv,
                    RefinanceType = loanTerms?.RefinanceType,
                    RefinanceProgram = loanTerms?.RefinanceProgram,
                    ExistingLiensAmount = loanTerms?.ExistingLiensAmount,
                    Status = loan.Status.ToString()
                },
                SubjectProperty = subjectProperty == null
                    ? null
                    : new ManualSubjectPropertySnapshotResponse
                    {
                        AddressLineText = subjectProperty.AddressLineText,
                        UnitIdentifier = subjectProperty.UnitIdentifier,
                        CityName = subjectProperty.CityName,
                        StateCode = subjectProperty.StateCode,
                        PostalCode = subjectProperty.PostalCode,
                        County = subjectProperty.County,
                        PropertyUsageType = subjectProperty.PropertyUsageType,
                        PropertyType = subjectProperty.PropertyType,
                        ConstructionMethod = subjectProperty.ConstructionMethod,
                        Occupancy = subjectProperty.Occupancy,
                        FinancedUnitCount = subjectProperty.FinancedUnitCount,
                        MixedUseProperty = subjectProperty.MixedUseProperty,
                        NonOccupantCoBorrower = subjectProperty.NonOccupantCoBorrower,
                        CommunityPropertyState = subjectProperty.CommunityPropertyState,
                        EnergyImprovement = subjectProperty.EnergyImprovement,
                        SolarLienPriority = subjectProperty.SolarLienPriority,
                        ConversionOfContract = subjectProperty.ConversionOfContract,
                        Renovation = subjectProperty.Renovation,
                        ConstructionLoan = subjectProperty.ConstructionLoan,
                        LotAcquiredDate = subjectProperty.LotAcquiredDate,
                        OriginalCostOfLot = subjectProperty.OriginalCostOfLot,
                        AttachmentType = subjectProperty.AttachmentType,
                        StructureType = subjectProperty.StructureType,
                        DesignType = subjectProperty.DesignType,
                        YearBuilt = subjectProperty.YearBuilt,
                        Acreage = subjectProperty.Acreage,
                        Improvements = subjectProperty.Improvements,
                        ImprovementCosts = subjectProperty.ImprovementCosts,
                        AccessoryDwellingUnitCount = subjectProperty.AccessoryDwellingUnitCount,
                        ConstructionStatus = subjectProperty.ConstructionStatus,
                        ProjectName = subjectProperty.ProjectName,
                        CondoProjectManagerId = subjectProperty.CondoProjectManagerId,
                        LegalDescription = subjectProperty.LegalDescription,
                        PropertyEstimatedValueAmount = subjectProperty.PropertyEstimatedValueAmount
                    },
                HousingExpenses = housingExpenses
                    .Select(x => new ManualHousingExpenseSnapshotResponse
                    {
                        HousingExpenseType = x.HousingExpenseType,
                        PresentHousingExpenseAmount = x.PresentHousingExpenseAmount,
                        ProposedHousingExpenseAmount = x.ProposedHousingExpenseAmount,
                        Mode = x.Mode,
                        Factor = x.Factor,
                        ValueAmount = x.ValueAmount,
                        SourceType = x.SourceType,
                        Description = x.Description,
                        InitialAnnualRate = x.InitialAnnualRate,
                        InitialMonthlyAmount = x.InitialMonthlyAmount,
                        RenewalAnnualRate = x.RenewalAnnualRate,
                        RenewalMonthlyAmount = x.RenewalMonthlyAmount,
                        Included = x.Included,
                        DisplayOrder = x.DisplayOrder
                    })
                    .ToList(),
                Borrowers = borrowers.Select(borrower => new ManualBorrowerSnapshotResponse
                {
                    Id = borrower.Id,
                    BorrowerType = borrower.BorrowerType,
                    FirstName = borrower.FirstName,
                    MiddleName = borrower.MiddleName,
                    LastName = borrower.LastName,
                    Suffix = borrower.Suffix,
                    Nickname = borrower.Nickname,
                    SsnItin = borrower.SsnItin,
                    Email = borrower.Email,
                    CellPhone = borrower.CellPhone,
                    HomePhone = borrower.HomePhone,
                    WorkPhone = borrower.WorkPhone,
                    WorkPhoneExtension = borrower.WorkPhoneExtension,
                    MaritalStatus = borrower.MaritalStatus,
                    DateOfBirth = borrower.DateOfBirth,
                    EstimatedCreditScore = borrower.EstimatedCreditScore,
                    EConsentAuthorized = borrower.EConsentAuthorized,
                    CreditPullAuthorized = borrower.CreditPullAuthorized,
                    NumberOfDependents = borrower.NumberOfDependents,
                    DependentAges = borrower.DependentAges,
                    MailingAddressSameAsCurrent = ResolveMailingAddressSameAsCurrent(
                        borrower,
                        addresses.Where(x => x.BorrowerId == borrower.Id).ToList()),
                    OtherLanguageDescription = borrower.OtherLanguageDescription,
                    LanguagePreferences = DeserializeLanguagePreferences(borrower.LanguagePreferences),
                    Addresses = addresses.Where(x => x.BorrowerId == borrower.Id).Select(x => new ManualBorrowerAddressSnapshotResponse
                    {
                        AddressType = x.AddressType,
                        StreetAddress = x.StreetAddress,
                        City = x.City,
                        State = x.State,
                        ZipCode = x.ZipCode,
                        OccupancyType = x.OccupancyType,
                        YearsSpent = x.YearsSpent,
                        MonthsSpent = x.MonthsSpent
                    }).ToList(),
                    Employments = employments.Where(x => x.BorrowerId == borrower.Id).Select(x => new ManualBorrowerEmploymentSnapshotResponse
                    {
                        EmployerName = x.EmployerName,
                        EmploymentStatusType = x.EmploymentStatusType,
                        EmploymentStartDate = x.EmploymentStartDate,
                        EmploymentPositionDescription = x.EmploymentPositionDescription
                    }).ToList(),
                    Incomes = incomes.Where(x => x.BorrowerId == borrower.Id).Select(x => new ManualBorrowerIncomeSnapshotResponse
                    {
                        IncomeType = x.IncomeType,
                        CurrentIncomeMonthlyTotalAmount = x.CurrentIncomeMonthlyTotalAmount
                    }).ToList(),
                    Assets = assets.Where(x => x.BorrowerId == borrower.Id).Select(x => new ManualBorrowerAssetSnapshotResponse
                    {
                        AssetType = x.AssetType,
                        AssetCashOrMarketValueAmount = x.AssetCashOrMarketValueAmount
                    }).ToList(),
                    Liabilities = liabilities.Where(x => x.BorrowerId == borrower.Id).Select(x => new ManualBorrowerLiabilitySnapshotResponse
                    {
                        LiabilityType = x.LiabilityType,
                        MonthlyPaymentAmount = x.MonthlyPaymentAmount,
                        UPBAmount = x.UPBAmount
                    }).ToList(),
                    Declarations = declarations.Where(x => x.BorrowerId == borrower.Id).Select(x => new ManualBorrowerDeclarationSnapshotResponse
                    {
                        DeclarationType = x.DeclarationType,
                        IsAffirmative = x.IsAffirmative,
                        Explanation = x.Explanation
                    }).ToList(),
                    GovernmentMonitoring = governmentMonitorings
                        .Where(x => x.BorrowerId == borrower.Id)
                        .Select(x => new ManualGovernmentMonitoringSnapshotResponse
                        {
                            ToBeCompletedByFinancialInstitution = x.ToBeCompletedByFinancialInstitution,
                            EthnicityType = x.EthnicityType,
                            EthnicityOriginOther = x.EthnicityOriginOther,
                            RaceType = x.RaceType,
                            RaceOther = x.RaceOther,
                            SexType = x.SexType,
                            CollectionMethodType = x.CollectionMethodType,
                            CollectedByVisualObservation = x.CollectedByVisualObservation,
                            CollectedEthnicityByObservation = x.EthnicityVisualObservationIndicator,
                            CollectedRaceByObservation = x.RaceVisualObservationIndicator,
                            CollectedSexByObservation = x.SexVisualObservationIndicator,
                            HmdaMonitoringType = x.HmdaMonitoringType,
                            EthnicityVisualObservationIndicator = x.EthnicityVisualObservationIndicator,
                            RaceVisualObservationIndicator = x.RaceVisualObservationIndicator,
                            SexVisualObservationIndicator = x.SexVisualObservationIndicator
                        })
                        .FirstOrDefault(),
                    MilitaryInformation = militaryInfos
                        .Where(x => x.BorrowerId == borrower.Id)
                        .Select(x => new ManualBorrowerMilitaryInfoSnapshotResponse
                        {
                            MilitaryStatus = x.MilitaryStatus,
                            ActiveDuty = x.ActiveDuty,
                            Veteran = x.Veteran,
                            Reserves = x.Reserves,
                            NationalGuard = x.NationalGuard,
                            ServiceBranch = x.ServiceBranch,
                            ServiceStartDate = x.ServiceStartDate,
                            ServiceEndDate = x.ServiceEndDate,
                            ProjectedExpirationDate = x.ProjectedExpirationDate,
                            SurvivorSpouse = x.SurvivorSpouse,
                            VaEligible = x.VaEligible,
                            VaEligibilityStatus = x.VaEligibilityStatus,
                            VaFirstTimeUse = x.VaFirstTimeUse,
                            VaFundingFeeExempt = x.VaFundingFeeExempt
                        })
                        .FirstOrDefault()
                }).ToList(),
                Fico = loanTerms?.LoanFico ?? fico,
                Ltv = loanTerms?.Ltv,
                Cltv = loanTerms?.Cltv,
                Hcltv = loanTerms?.Hcltv,
                Ftc = null,
                FirstTimeHomebuyer = null,
                EstimatedClosingDate = null,
                ClosingDate = null,
                AppraisedValue = subjectProperty?.PropertyEstimatedValueAmount,
                TotalLoanAmount = loanTerms?.TotalLoanAmount ?? loan.LoanAmount,
                NoteRatePercent = loanTerms?.NoteRatePercent,
                PropertyAddressSummary = string.IsNullOrWhiteSpace(propertyAddressSummary) ? null : propertyAddressSummary,
                LockStatus = null,
                ProductName = null,
                LenderName = lenderName,
                LoanStructure = new ManualLoanStructureEnrichmentResponse
                {
                    PurchasePrice = loanTerms?.PurchasePrice ?? (loanTerms?.Ltv is > 0 and <= 100 && (loanTerms?.TotalLoanAmount ?? loan.LoanAmount) > 0
                        ? ((loanTerms?.TotalLoanAmount ?? loan.LoanAmount) / (loanTerms.Ltv.Value / 100m))
                        : null),
                    DownPaymentPercent = loanTerms?.DownPaymentPercent ?? (loanTerms?.Ltv is > 0 and <= 100
                        ? Math.Max(0m, 100m - loanTerms.Ltv.Value)
                        : null),
                    DownPaymentAmount = loanTerms?.DownPaymentAmount ?? (loanTerms?.Ltv is > 0 and < 100 && (loanTerms?.TotalLoanAmount ?? loan.LoanAmount) > 0
                        ? ((loanTerms?.TotalLoanAmount ?? loan.LoanAmount) * (Math.Max(0m, 100m - loanTerms.Ltv.Value) / (100m - Math.Max(0m, 100m - loanTerms.Ltv.Value))))
                        : loanTerms?.Ltv == 100 ? 0m : null),
                    BaseLoanAmount = loanTerms?.BaseLoanAmount ?? loanTerms?.NoteAmount ?? loan.LoanAmount,
                    TotalLoanAmount = loanTerms?.TotalLoanAmount ?? loan.LoanAmount,
                    SubordinateLienAmount = loanTerms?.ExistingLiensAmount,
                    QualifyingRate = loanTerms?.QualifyingRate,
                    InterestOnly = loanTerms?.InterestOnly,
                    InterestOnlyTermMonths = loanTerms?.InterestOnlyTermMonths,
                    InterestRateBuydown = loanTerms?.InterestRateBuydown,
                    ImpoundWaiver = loanTerms?.ImpoundWaiver,
                    LoanFico = loanTerms?.LoanFico,
                    NoFico = loanTerms?.NoFico,
                    Ltv = loanTerms?.Ltv,
                    Cltv = loanTerms?.Cltv,
                    Hcltv = loanTerms?.Hcltv,
                    RefinanceType = loanTerms?.RefinanceType,
                    RefinanceProgram = loanTerms?.RefinanceProgram,
                    ExistingLiensAmount = loanTerms?.ExistingLiensAmount,
                    LpaOffering = loanTerms?.LpaOffering,
                    AlternateDoc = loanTerms?.AlternateDoc,
                    TemporaryBuydown = loanTerms?.TemporaryBuydown,
                    BusinessPurpose = loanTerms?.BusinessPurpose,
                    BridgeLoan = loanTerms?.BridgeLoan,
                    AssetDepletion = loanTerms?.AssetDepletion,
                    InvestmentIncomeMode = loanTerms?.InvestmentIncomeMode,
                    MonthlyGrossRentalIncome = loanTerms?.MonthlyGrossRentalIncome,
                    RentalPercentage = loanTerms?.RentalPercentage,
                    TotalCalculatedRent = loanTerms?.TotalCalculatedRent,
                    TotalExpenses = loanTerms?.TotalExpenses,
                    NetSubjectIncome = loanTerms?.NetSubjectIncome,
                    PurchaseCredits = purchaseCredits.Sum(x => x.Amount),
                    ProjectedReserveAmount = loanTerms?.ProjectedReserveMonths.HasValue == true
                        ? (decimal?)loanTerms.ProjectedReserveMonths.Value
                        : null,
                    ProjectedReserveMonths = loanTerms?.ProjectedReserveMonths,
                    FirstMortgage = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "FirstMortgage")?.ProposedHousingExpenseAmount,
                    OtherFinancing = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "OtherFinancing")?.ProposedHousingExpenseAmount,
                    OtherFinancingMode = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "OtherFinancing")?.Mode,
                    OtherFinancingFactor = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "OtherFinancing")?.Factor,
                    OtherFinancingValue = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "OtherFinancing")?.ValueAmount,
                    Hoi = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "HOI")?.ProposedHousingExpenseAmount,
                    HoiMode = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "HOI")?.Mode,
                    HoiFactor = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "HOI")?.Factor,
                    HoiSource = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "HOI")?.SourceType,
                    Supplemental = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "Supplemental")?.ProposedHousingExpenseAmount,
                    SupplementalMode = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "Supplemental")?.Mode,
                    SupplementalFactor = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "Supplemental")?.Factor,
                    SupplementalSource = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "Supplemental")?.SourceType,
                    PropertyTaxes = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "PropertyTaxes")?.ProposedHousingExpenseAmount,
                    PropertyTaxesMode = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "PropertyTaxes")?.Mode,
                    PropertyTaxesFactor = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "PropertyTaxes")?.Factor,
                    PropertyTaxesSource = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "PropertyTaxes")?.SourceType,
                    MiMode = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "MI")?.Mode,
                    MiInitialAnnualRate = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "MI")?.InitialAnnualRate,
                    MiInitialMonthly = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "MI")?.InitialMonthlyAmount,
                    MiRenewalAnnualRate = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "MI")?.RenewalAnnualRate,
                    MiRenewalMonthly = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "MI")?.RenewalMonthlyAmount,
                    AssociationDues = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "AssociationDues")?.ProposedHousingExpenseAmount,
                    OtherDescription = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "Other")?.Description,
                    OtherAmount = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "Other")?.ProposedHousingExpenseAmount,
                    TotalPiti = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "TotalPITI")?.ProposedHousingExpenseAmount,
                    OwnerOfExistingMortgage = null,
                    AusRunType = latestAusRun?.RunType.ToString(),
                    AusStatus = latestAusRun?.Status.ToString(),
                    RecommendedAusProvider = latestAusRun?.RecommendedProvider?.ToString(),
                    DuRecommendation = duFinding?.Recommendation,
                    DuEligibility = duFinding?.Eligibility,
                    DuRiskClass = duFinding?.RiskClass,
                    LpaRecommendation = lpaFinding?.Recommendation,
                    LpaEligibility = lpaFinding?.Eligibility,
                    LpaRiskClass = lpaFinding?.RiskClass
                },
                TitleInfo = new ManualTitleInfoResponse
                {
                    Vesting = titleInfo?.TitleHeldInNameOf,
                    MannerTitleHeld = titleInfo?.MannerTitleHeld,
                    TitleHeldInNameOf = titleInfo?.TitleHeldInNameOf,
                    VestingToRead = titleInfo?.VestingToRead,
                    TrustInformation = titleInfo?.TrustInformation,
                    IndianCountryLandTenure = titleInfo?.IndianCountryLandTenure,
                    EstateHeldIn = titleInfo?.EstateHeldIn,
                    TitleCompany = titleInfo?.TitleInsuranceCompany,
                    TitleInsuranceCompany = titleInfo?.TitleInsuranceCompany,
                    TitleCommitmentNumber = titleInfo?.TitleCommitmentNumber,
                    TitlePolicyNumber = titleInfo?.TitlePolicyNumber,
                    VestingEntityType = titleInfo?.VestingEntityType,
                    TitleNotes = titleInfo?.TitleNotes,
                    Homestead = titleInfo?.Homestead ?? false,
                    TitleCurative = titleInfo?.TitleCurative ?? false,
                    PowerOfAttorney = titleInfo?.PowerOfAttorney ?? false,
                    RecordedEasement = titleInfo?.RecordedEasement ?? false,
                    TitleOfficer = titleAgent?.FullName,
                    TitleContact = titleAgent?.FullName ?? titleAgent?.Email ?? titleAgent?.MobilePhone ?? titleAgent?.WorkPhone,
                    EscrowCompany = null,
                    SettlementAgent = null
                },
                DownPaymentSources = downPaymentSources.Select(x => new ManualDownPaymentSourceResponse
                {
                    Id = x.Id,
                    SourceType = x.SourceType,
                    Amount = x.Amount,
                    Included = x.Included,
                    DisplayOrder = x.DisplayOrder
                }).ToList(),
                PurchaseCredits = purchaseCredits.Select(x => new ManualPurchaseCreditResponse
                {
                    Id = x.Id,
                    CreditType = x.CreditType,
                    SourceType = x.SourceType,
                    Amount = x.Amount,
                    DisplayOrder = x.DisplayOrder
                }).ToList(),
                SubordinateLiens = subordinateLiens.Select(x => new ManualSubordinateLienResponse
                {
                    Id = x.Id,
                    LienType = x.LienType,
                    LienName = x.LienName,
                    MonthlyPaymentAmount = x.MonthlyPaymentAmount,
                    LoanAmount = x.LoanAmount,
                    SourceType = x.SourceType,
                    DisplayOrder = x.DisplayOrder
                }).ToList(),
                HousingExpenseInputs = new ManualHousingExpenseInputsResponse
                {
                    OtherFinancing = new ManualCalcInputResponse
                    {
                        IsCalc = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "OtherFinancing")?.Included ?? false,
                        Factor = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "OtherFinancing")?.Mode ?? "Monthly",
                        Value = (housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "OtherFinancing")?.ProposedHousingExpenseAmount ?? 0m).ToString("0.##")
                    },
                    Hoi = new ManualSourceCalcInputResponse
                    {
                        IsCalc = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "HOI")?.Included ?? false,
                        Factor = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "HOI")?.Mode ?? "Monthly",
                        Value = (housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "HOI")?.ProposedHousingExpenseAmount ?? 0m).ToString("0.##"),
                        Percent = string.Empty,
                        Source = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "HOI")?.SourceType ?? "Appraised Value"
                    },
                    Supplemental = new ManualSourceCalcInputResponse
                    {
                        IsCalc = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "Supplemental")?.Included ?? false,
                        Factor = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "Supplemental")?.Mode ?? "Monthly",
                        Value = (housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "Supplemental")?.ProposedHousingExpenseAmount ?? 0m).ToString("0.##"),
                        Percent = string.Empty,
                        Source = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "Supplemental")?.SourceType ?? "Appraised Value"
                    },
                    PropertyTaxes = new ManualSourceCalcInputResponse
                    {
                        IsCalc = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "PropertyTaxes")?.Included ?? false,
                        Factor = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "PropertyTaxes")?.Mode ?? "Monthly",
                        Value = (housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "PropertyTaxes")?.ProposedHousingExpenseAmount ?? 0m).ToString("0.##"),
                        Percent = string.Empty,
                        Source = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "PropertyTaxes")?.SourceType ?? "Appraised Value"
                    },
                    Mi = new ManualMiInputResponse
                    {
                        Mode = miHousingRow?.Mode ?? string.Empty,
                        Factor = "Monthly",
                        Value = ((miHousingRow?.InitialMonthlyAmount ?? 0m) > 0m
                            ? (miHousingRow?.InitialMonthlyAmount ?? 0m)
                            : (miHousingRow?.ProposedHousingExpenseAmount ?? 0m)).ToString("0.##"),
                        Source = "Total Loan Amount",
                        MortgageInsurance = new ManualMiRatesInputResponse
                        {
                            InitialAnnualRate = (miHousingRow?.InitialAnnualRate ?? 0m) == 0m ? string.Empty : (miHousingRow?.InitialAnnualRate ?? 0m).ToString("0.##"),
                            InitialMonthly = (miHousingRow?.InitialMonthlyAmount ?? 0m) == 0m ? string.Empty : (miHousingRow?.InitialMonthlyAmount ?? 0m).ToString("0.##"),
                            RenewalAnnualRate = (miHousingRow?.RenewalAnnualRate ?? 0m) == 0m ? string.Empty : (miHousingRow?.RenewalAnnualRate ?? 0m).ToString("0.##"),
                            RenewalMonthly = (miHousingRow?.RenewalMonthlyAmount ?? 0m) == 0m ? string.Empty : (miHousingRow?.RenewalMonthlyAmount ?? 0m).ToString("0.##")
                        }
                    },
                    AssociationDues = new ManualValueInputResponse
                    {
                        Value = (housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "AssociationDues")?.ProposedHousingExpenseAmount ?? 0m).ToString("0.##")
                    },
                    Other = new ManualOtherInputResponse
                    {
                        Value = (housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "Other")?.ProposedHousingExpenseAmount ?? 0m).ToString("0.##"),
                        Description = housingExpenses.FirstOrDefault(x => x.HousingExpenseType == "Other")?.Description ?? string.Empty
                    }
                },
                BorrowerApplications = BuildBorrowerApplicationsSnapshot(borrowers)
            };
        }

        private static IReadOnlyDictionary<int, (int ApplicationNumber, int BorrowerOrder)> BuildBorrowerApplicationAssignments(
            IReadOnlyList<ManualBorrowerApplicationRequest>? borrowerApplications)
        {
            var assignments = new Dictionary<int, (int ApplicationNumber, int BorrowerOrder)>();

            if (borrowerApplications == null || borrowerApplications.Count == 0)
                return assignments;

            for (var appIndex = 0; appIndex < borrowerApplications.Count; appIndex++)
            {
                var application = borrowerApplications[appIndex];
                var applicationNumber = application.ApplicationNumber > 0
                    ? application.ApplicationNumber
                    : appIndex + 1;

                if (applicationNumber <= 0)
                    continue;

                for (var index = 0; index < application.BorrowerIds.Count; index++)
                {
                    if (!TryParseBorrowerId(application.BorrowerIds[index], out var borrowerId) || borrowerId <= 0)
                        continue;

                    assignments[borrowerId] = (applicationNumber, index + 1);
                }
            }

            return assignments;
        }

        private static bool TryParseBorrowerId(JsonElement borrowerToken, out int borrowerId)
        {
            borrowerId = 0;

            if (borrowerToken.ValueKind == JsonValueKind.Number)
                return borrowerToken.TryGetInt32(out borrowerId);

            if (borrowerToken.ValueKind == JsonValueKind.String)
                return int.TryParse(borrowerToken.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out borrowerId);

            if (borrowerToken.ValueKind == JsonValueKind.Object)
            {
                if (TryParseBorrowerIdProperty(borrowerToken, "id", out borrowerId))
                    return true;

                if (TryParseBorrowerIdProperty(borrowerToken, "borrowerId", out borrowerId))
                    return true;

                if (TryParseBorrowerIdProperty(borrowerToken, "value", out borrowerId))
                    return true;
            }

            return false;
        }

        private static bool TryParseBorrowerIdProperty(JsonElement token, string propertyName, out int borrowerId)
        {
            borrowerId = 0;

            if (!token.TryGetProperty(propertyName, out var property))
                return false;

            if (property.ValueKind == JsonValueKind.Number)
                return property.TryGetInt32(out borrowerId);

            if (property.ValueKind == JsonValueKind.String)
                return int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out borrowerId);

            return false;
        }

        private static string? SerializeLanguagePreferences(JsonElement? languagePreferences)
        {
            if (!languagePreferences.HasValue)
                return null;

            var value = languagePreferences.Value;

            if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                return null;

            return value.GetRawText();
        }

        private static JsonElement? DeserializeLanguagePreferences(string? languagePreferences)
        {
            if (string.IsNullOrWhiteSpace(languagePreferences))
                return null;

            try
            {
                return JsonSerializer.Deserialize<JsonElement>(languagePreferences);
            }
            catch (JsonException)
            {
                return JsonSerializer.SerializeToElement(languagePreferences);
            }
        }

        private static bool? ResolveMailingAddressSameAsCurrent(ManualBorrowerRequest borrowerRequest)
        {
            if (borrowerRequest.MailingAddressSameAsCurrent.HasValue)
                return borrowerRequest.MailingAddressSameAsCurrent;

            var hasMailingAddress = borrowerRequest.Addresses.Any(address =>
                string.Equals(address.AddressType, "Mailing", StringComparison.OrdinalIgnoreCase));

            if (hasMailingAddress)
                return false;

            return null;
        }

        private static bool? ResolveMailingAddressSameAsCurrent(Borrower borrower, List<BorrowerAddress> borrowerAddresses)
        {
            if (borrower.MailingAddressSameAsCurrent.HasValue)
                return borrower.MailingAddressSameAsCurrent.Value;

            var hasMailingAddress = borrowerAddresses.Any(address =>
                string.Equals(address.AddressType, "Mailing", StringComparison.OrdinalIgnoreCase));
            if (hasMailingAddress)
                return false;

            if (borrowerAddresses.Count > 0)
                return true;

            return null;
        }

        private static DateTime? ParseDateOfBirth(JsonElement? dateOfBirth)
        {
            if (!dateOfBirth.HasValue)
                return null;

            var value = dateOfBirth.Value;

            if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                return null;

            if (value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (string.IsNullOrWhiteSpace(text))
                    return null;

                if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDate))
                    return parsedDate;

                if (DateTime.TryParseExact(text, new[] { "yyyy-MM-dd", "MM/dd/yyyy", "M/d/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out parsedDate))
                    return parsedDate;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var unixSeconds))
            {
                try
                {
                    return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
                }
                catch (ArgumentOutOfRangeException)
                {
                    return null;
                }
            }

            return null;
        }

        private static List<ManualBorrowerApplicationSnapshotResponse> BuildBorrowerApplicationsSnapshot(IReadOnlyList<Borrower> borrowers)
        {
            var hasPersistedGrouping = borrowers.Any(x => x.ApplicationNumber.HasValue);

            if (!hasPersistedGrouping)
                return [];

            return borrowers
                .Where(x => x.ApplicationNumber.HasValue)
                .GroupBy(x => x.ApplicationNumber!.Value)
                .OrderBy(x => x.Key)
                .Select(group => new ManualBorrowerApplicationSnapshotResponse
                {
                    ApplicationNumber = group.Key,
                    BorrowerIds = group
                        .OrderBy(x => x.ApplicationBorrowerOrder ?? int.MaxValue)
                        .Select(x => x.Id)
                        .ToList()
                })
                .ToList();
        }

        private async Task<string> GenerateLoanNumberAsync(CancellationToken cancellationToken)
        {
            string loanNumber;

            do
            {
                var number = RandomNumberGenerator.GetInt32(10000000, 100000000);
                loanNumber = number.ToString();
            }
            while (await _context.Loans.AnyAsync(l => l.LoanNumber == loanNumber, cancellationToken));

            return loanNumber;
        }
    }
}
