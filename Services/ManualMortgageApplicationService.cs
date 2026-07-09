using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;
using System.Security.Cryptography;

namespace PrestexaAPI.Services
{
    public class ManualMortgageApplicationService
    {
        private readonly AppDbContext _context;

        public ManualMortgageApplicationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> CreateAsync(
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
                LoanAmount = request.Loan.NoteAmount,
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

        public async Task<object?> GetAsync(
            string loanNumber,
            string companyNmlsNumber,
            CancellationToken cancellationToken = default)
        {
            return await BuildApplicationResponseAsync(loanNumber, companyNmlsNumber, cancellationToken);
        }

        public async Task<object?> UpdateAsync(
            string loanNumber,
            ManualMortgageApplicationRequest request,
            string companyNmlsNumber,
            CancellationToken cancellationToken = default)
        {
            var loan = await _context.Loans
                .FirstOrDefaultAsync(x => x.LoanNumber == loanNumber && x.CompanyNmlsNumber == companyNmlsNumber, cancellationToken);

            if (loan == null)
                return null;

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            loan.Subject_Street_Address = request.SubjectProperty.AddressLineText;
            loan.Subject_City = request.SubjectProperty.CityName;
            loan.Subject_State = request.SubjectProperty.StateCode;
            loan.Subject_ZipCode = request.SubjectProperty.PostalCode;
            loan.LoanAmount = request.Loan.NoteAmount;
            loan.UpdatedAt = DateTime.UtcNow;

            await ClearAggregateAsync(loan.Id, companyNmlsNumber, cancellationToken);
            await SaveAggregateAsync(loan, request, companyNmlsNumber, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return await BuildApplicationResponseAsync(loanNumber, companyNmlsNumber, cancellationToken);
        }

        private async Task SaveAggregateAsync(
            Loan loan,
            ManualMortgageApplicationRequest request,
            string companyNmlsNumber,
            CancellationToken cancellationToken)
        {
            var loanTerms = new LoanTerms
            {
                CompanyNmlsNumber = companyNmlsNumber,
                LoanId = loan.Id,
                NoteAmount = request.Loan.NoteAmount,
                NoteRatePercent = request.Loan.NoteRatePercent,
                LoanAmortizationPeriodCount = request.Loan.LoanAmortizationPeriodCount,
                LoanAmortizationType = request.Loan.LoanAmortizationType,
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
                CityName = request.SubjectProperty.CityName,
                StateCode = request.SubjectProperty.StateCode,
                PostalCode = request.SubjectProperty.PostalCode,
                PropertyUsageType = request.SubjectProperty.PropertyUsageType,
                FinancedUnitCount = request.SubjectProperty.FinancedUnitCount,
                PropertyEstimatedValueAmount = request.SubjectProperty.PropertyEstimatedValueAmount,
                CreatedAt = DateTime.UtcNow
            };
            _context.SubjectProperties.Add(subjectProperty);

            await _context.SaveChangesAsync(cancellationToken);

            foreach (var housingExpenseRequest in request.HousingExpenses)
            {
                _context.HousingExpenses.Add(new HousingExpense
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    LoanId = loan.Id,
                    HousingExpenseType = housingExpenseRequest.HousingExpenseType,
                    PresentHousingExpenseAmount = housingExpenseRequest.PresentHousingExpenseAmount,
                    ProposedHousingExpenseAmount = housingExpenseRequest.ProposedHousingExpenseAmount,
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var borrowerRequest in request.Borrowers)
            {
                var borrower = new Borrower
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    LoanId = loan.Id,
                    BorrowerType = borrowerRequest.BorrowerType,
                    FirstName = borrowerRequest.FirstName,
                    MiddleName = borrowerRequest.MiddleName,
                    LastName = borrowerRequest.LastName,
                    Email = borrowerRequest.Email,
                    CellPhone = borrowerRequest.CellPhone,
                    HomePhone = borrowerRequest.HomePhone,
                    WorkPhone = borrowerRequest.WorkPhone,
                    MaritalStatus = borrowerRequest.MaritalStatus,
                    DateOfBirth = borrowerRequest.DateOfBirth,
                    EstimatedCreditScore = borrowerRequest.EstimatedCreditScore,
                    EConsentAuthorized = borrowerRequest.EConsentAuthorized,
                    CreditPullAuthorized = borrowerRequest.CreditPullAuthorized,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Borrowers.Add(borrower);
                await _context.SaveChangesAsync(cancellationToken);

                foreach (var addressRequest in borrowerRequest.Addresses)
                {
                    _context.BorrowerAddresses.Add(new BorrowerAddress
                    {
                        CompanyNmlsNumber = companyNmlsNumber,
                        BorrowerId = borrower.Id,
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
                    var employment = new BorrowerEmployment
                    {
                        CompanyNmlsNumber = companyNmlsNumber,
                        BorrowerId = borrower.Id,
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
                        BorrowerId = borrower.Id,
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
                        BorrowerId = borrower.Id,
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
                        BorrowerId = borrower.Id,
                        LiabilityType = liabilityRequest.LiabilityType,
                        MonthlyPaymentAmount = liabilityRequest.MonthlyPaymentAmount,
                        UPBAmount = liabilityRequest.UPBAmount,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                foreach (var declarationRequest in borrowerRequest.Declarations)
                {
                    _context.BorrowerDeclarations.Add(new BorrowerDeclaration
                    {
                        CompanyNmlsNumber = companyNmlsNumber,
                        LoanId = loan.Id,
                        BorrowerId = borrower.Id,
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
                        LoanId = loan.Id,
                        BorrowerId = borrower.Id,
                        EthnicityType = borrowerRequest.GovernmentMonitoring.EthnicityType,
                        RaceType = borrowerRequest.GovernmentMonitoring.RaceType,
                        SexType = borrowerRequest.GovernmentMonitoring.SexType,
                        CollectionMethodType = borrowerRequest.GovernmentMonitoring.CollectionMethodType,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task ClearAggregateAsync(int loanId, string companyNmlsNumber, CancellationToken cancellationToken)
        {
            var housingExpenses = await _context.HousingExpenses
                .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.HousingExpenses.RemoveRange(housingExpenses);

            var declarations = await _context.BorrowerDeclarations
                .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.BorrowerDeclarations.RemoveRange(declarations);

            var monitorings = await _context.GovernmentMonitorings
                .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.GovernmentMonitorings.RemoveRange(monitorings);

            var loanTerms = await _context.LoanTerms
                .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.LoanTerms.RemoveRange(loanTerms);

            var properties = await _context.SubjectProperties
                .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.SubjectProperties.RemoveRange(properties);

            var borrowers = await _context.Borrowers
                .Where(x => x.LoanId == loanId && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);
            _context.Borrowers.RemoveRange(borrowers);

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<object?> BuildApplicationResponseAsync(
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
                .Select(x => new
                {
                    x.HousingExpenseType,
                    x.PresentHousingExpenseAmount,
                    x.ProposedHousingExpenseAmount
                })
                .ToListAsync(cancellationToken);

            var borrowers = await _context.Borrowers
                .Where(x => x.LoanId == loan.Id && x.CompanyNmlsNumber == companyNmlsNumber)
                .OrderBy(x => x.BorrowerType)
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
                .Where(x => x.LoanId == loan.Id && borrowerIds.Contains(x.BorrowerId) && x.CompanyNmlsNumber == companyNmlsNumber)
                .ToListAsync(cancellationToken);

            return new
            {
                loanNumber = loan.LoanNumber,
                loan = new
                {
                    loanTerms?.LoanPurposeType,
                    loanTerms?.MortgageType,
                    loanTerms?.LienPriorityType,
                    noteAmount = loanTerms?.NoteAmount ?? loan.LoanAmount,
                    loanTerms?.NoteRatePercent,
                    loanTerms?.LoanAmortizationPeriodCount,
                    loanTerms?.LoanAmortizationType,
                    status = loan.Status.ToString()
                },
                subjectProperty = subjectProperty == null ? null : new
                {
                    subjectProperty.AddressLineText,
                    subjectProperty.CityName,
                    subjectProperty.StateCode,
                    subjectProperty.PostalCode,
                    subjectProperty.PropertyUsageType,
                    subjectProperty.FinancedUnitCount,
                    subjectProperty.PropertyEstimatedValueAmount
                },
                housingExpenses,
                borrowers = borrowers.Select(borrower => new
                {
                    borrower.Id,
                    borrower.BorrowerType,
                    borrower.FirstName,
                    borrower.MiddleName,
                    borrower.LastName,
                    borrower.Email,
                    borrower.CellPhone,
                    borrower.HomePhone,
                    borrower.WorkPhone,
                    borrower.MaritalStatus,
                    borrower.DateOfBirth,
                    borrower.EstimatedCreditScore,
                    borrower.EConsentAuthorized,
                    borrower.CreditPullAuthorized,
                    addresses = addresses.Where(x => x.BorrowerId == borrower.Id).Select(x => new
                    {
                        x.AddressType,
                        x.StreetAddress,
                        x.City,
                        x.State,
                        x.ZipCode,
                        x.OccupancyType,
                        x.YearsSpent,
                        x.MonthsSpent
                    }),
                    employments = employments.Where(x => x.BorrowerId == borrower.Id).Select(x => new
                    {
                        x.EmployerName,
                        x.EmploymentStatusType,
                        x.EmploymentStartDate,
                        x.EmploymentPositionDescription
                    }),
                    incomes = incomes.Where(x => x.BorrowerId == borrower.Id).Select(x => new
                    {
                        x.IncomeType,
                        x.CurrentIncomeMonthlyTotalAmount
                    }),
                    assets = assets.Where(x => x.BorrowerId == borrower.Id).Select(x => new
                    {
                        x.AssetType,
                        x.AssetCashOrMarketValueAmount
                    }),
                    liabilities = liabilities.Where(x => x.BorrowerId == borrower.Id).Select(x => new
                    {
                        x.LiabilityType,
                        x.MonthlyPaymentAmount,
                        x.UPBAmount
                    }),
                    declarations = declarations.Where(x => x.BorrowerId == borrower.Id).Select(x => new
                    {
                        x.DeclarationType,
                        x.IsAffirmative,
                        x.Explanation
                    }),
                    governmentMonitoring = governmentMonitorings
                        .Where(x => x.BorrowerId == borrower.Id)
                        .Select(x => new
                        {
                            x.EthnicityType,
                            x.RaceType,
                            x.SexType,
                            x.CollectionMethodType
                        })
                        .FirstOrDefault()
                })
            };
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
