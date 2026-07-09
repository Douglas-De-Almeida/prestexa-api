using Microsoft.EntityFrameworkCore;
using PrestexaAPI.Data;
using PrestexaAPI.Models;
using System.Text;
using System.Security.Cryptography;

namespace PrestexaAPI.Services.Mismo
{
    public class MismoImportService : IMismoImportService
    {
        private readonly AppDbContext _context;
        private readonly IMismoParserService _parser;

        public MismoImportService(
            AppDbContext context,
            IMismoParserService parser)
        {
            _context = context;
            _parser = parser;
        }

        public async Task<MismoImportResult> ImportAsync(
            Stream xmlStream,
            int userId,
            string companyNmlsNumber,
            int? branchId = null,
            int? sourceMismoFileId = null,
            bool allowDuplicate = false,
            CancellationToken cancellationToken = default)
        {
            await using var contentStream = new MemoryStream();
            await xmlStream.CopyToAsync(contentStream, cancellationToken);
            contentStream.Position = 0;

            var contentSha256 = ComputeSha256(contentStream.ToArray());

            var existingRecord = await _context.Set<MismoImportRecord>()
                .Include(x => x.Loan)
                .FirstOrDefaultAsync(x =>
                    x.CompanyNmlsNumber == companyNmlsNumber &&
                    x.ContentSha256 == contentSha256,
                    cancellationToken);

            if (existingRecord != null && !allowDuplicate)
            {
                return new MismoImportResult
                {
                    LoanId = existingRecord.LoanId,
                    LoanNumber = existingRecord.Loan.LoanNumber,
                    CompanyNmlsNumber = companyNmlsNumber,
                    MismoVersion = existingRecord.MismoVersion,
                    ContentSha256 = contentSha256,
                    IsDuplicate = true,
                    ExistingLoanId = existingRecord.LoanId,
                    ExistingLoanNumber = existingRecord.Loan.LoanNumber,
                    ImportedAtUtc = DateTime.UtcNow
                };
            }

            contentStream.Position = 0;
            var parsed = await _parser.ParseAsync(contentStream, cancellationToken);

            if (parsed.Borrowers.Count == 0)
                throw new InvalidOperationException("MISMO file must contain at least one borrower.");

            if (string.IsNullOrWhiteSpace(parsed.SubjectProperty.AddressLineText))
                throw new InvalidOperationException("MISMO file must contain a subject property address.");

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var loanNumber = await GenerateUniqueLoanNumberAsync(cancellationToken);

            var loan = new Loan
            {
                CompanyNmlsNumber = companyNmlsNumber,
                UserId = userId,
                LoanNumber = loanNumber,
                Subject_Street_Address = parsed.SubjectProperty.AddressLineText,
                Subject_City = parsed.SubjectProperty.CityName,
                Subject_State = parsed.SubjectProperty.StateCode,
                Subject_ZipCode = parsed.SubjectProperty.PostalCode,
                LoanAmount = parsed.LoanTerms.NoteAmount,
                Status = LoanStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync(cancellationToken);

            var loanTerms = new LoanTerms
            {
                CompanyNmlsNumber = companyNmlsNumber,
                LoanId = loan.Id,
                NoteAmount = parsed.LoanTerms.NoteAmount,
                NoteRatePercent = parsed.LoanTerms.NoteRatePercent,
                LoanAmortizationPeriodCount = parsed.LoanTerms.LoanAmortizationPeriodCount,
                LoanAmortizationType = parsed.LoanTerms.LoanAmortizationType,
                LoanPurposeType = parsed.LoanTerms.LoanPurposeType,
                MortgageType = parsed.LoanTerms.MortgageType,
                LienPriorityType = parsed.LoanTerms.LienPriorityType,
                CreatedAt = DateTime.UtcNow
            };

            _context.LoanTerms.Add(loanTerms);

            var subjectProperty = new SubjectProperty
            {
                CompanyNmlsNumber = companyNmlsNumber,
                LoanId = loan.Id,
                AddressLineText = parsed.SubjectProperty.AddressLineText,
                CityName = parsed.SubjectProperty.CityName,
                StateCode = parsed.SubjectProperty.StateCode,
                PostalCode = parsed.SubjectProperty.PostalCode,
                PropertyUsageType = parsed.SubjectProperty.PropertyUsageType,
                FinancedUnitCount = parsed.SubjectProperty.FinancedUnitCount,
                PropertyEstimatedValueAmount = parsed.SubjectProperty.PropertyEstimatedValueAmount,
                CreatedAt = DateTime.UtcNow
            };

            _context.SubjectProperties.Add(subjectProperty);

            var primaryBorrowerId = 0;

            foreach (var parsedBorrower in parsed.Borrowers)
            {
                var borrowerType = string.Equals(parsedBorrower.BorrowerRoleType, "CoBorrower", StringComparison.OrdinalIgnoreCase)
                    ? "CoBorrower"
                    : "PrimaryBorrower";

                var borrower = new Borrower
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    LoanId = loan.Id,
                    BorrowerType = borrowerType,
                    FirstName = parsedBorrower.FirstName,
                    LastName = parsedBorrower.LastName,
                    Email = parsedBorrower.EmailAddressText,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Borrowers.Add(borrower);
                await _context.SaveChangesAsync(cancellationToken);

                if (primaryBorrowerId == 0 && borrowerType == "PrimaryBorrower")
                    primaryBorrowerId = borrower.Id;

                if (parsedBorrower.CurrentAddress != null)
                {
                    _context.BorrowerAddresses.Add(new BorrowerAddress
                    {
                        CompanyNmlsNumber = companyNmlsNumber,
                        BorrowerId = borrower.Id,
                        AddressType = parsedBorrower.CurrentAddress.BorrowerResidencyType,
                        StreetAddress = parsedBorrower.CurrentAddress.AddressLineText,
                        City = parsedBorrower.CurrentAddress.CityName,
                        State = parsedBorrower.CurrentAddress.StateCode,
                        ZipCode = parsedBorrower.CurrentAddress.PostalCode,
                        MonthsSpent = parsedBorrower.CurrentAddress.ResidencyDurationMonthsCount
                    });
                }

                var firstEmploymentId = (int?)null;

                if (parsedBorrower.Employments.Count > 0)
                {
                    foreach (var parsedEmployment in parsedBorrower.Employments)
                    {
                        var employment = new BorrowerEmployment
                        {
                            CompanyNmlsNumber = companyNmlsNumber,
                            BorrowerId = borrower.Id,
                            EmployerName = parsedEmployment.EmployerName,
                            EmploymentStatusType = parsedEmployment.EmploymentStatusType,
                            EmploymentStartDate = parsedEmployment.EmploymentStartDate,
                            EmploymentPositionDescription = parsedEmployment.EmploymentPositionDescription,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.BorrowerEmployments.Add(employment);
                        await _context.SaveChangesAsync(cancellationToken);

                        firstEmploymentId ??= employment.Id;
                    }
                }

                foreach (var parsedIncome in parsedBorrower.Incomes)
                {
                    _context.BorrowerIncomes.Add(new BorrowerIncome
                    {
                        CompanyNmlsNumber = companyNmlsNumber,
                        BorrowerId = borrower.Id,
                        BorrowerEmploymentId = firstEmploymentId,
                        IncomeType = parsedIncome.IncomeType,
                        CurrentIncomeMonthlyTotalAmount = parsedIncome.CurrentIncomeMonthlyTotalAmount,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (primaryBorrowerId == 0)
            {
                primaryBorrowerId = await _context.Borrowers
                    .Where(x => x.LoanId == loan.Id)
                    .Select(x => x.Id)
                    .FirstAsync(cancellationToken);
            }

            foreach (var parsedAsset in parsed.Assets)
            {
                _context.BorrowerAssets.Add(new BorrowerAsset
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    BorrowerId = primaryBorrowerId,
                    AssetType = parsedAsset.AssetType,
                    AssetCashOrMarketValueAmount = parsedAsset.AssetCashOrMarketValueAmount,
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var parsedLiability in parsed.Liabilities)
            {
                _context.BorrowerLiabilities.Add(new BorrowerLiability
                {
                    CompanyNmlsNumber = companyNmlsNumber,
                    BorrowerId = primaryBorrowerId,
                    LiabilityType = parsedLiability.LiabilityType,
                    MonthlyPaymentAmount = parsedLiability.MonthlyPaymentAmount,
                    UPBAmount = parsedLiability.UPBAmount,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            _context.Set<MismoImportRecord>().Add(new MismoImportRecord
            {
                CompanyNmlsNumber = companyNmlsNumber,
                ContentSha256 = contentSha256,
                LoanId = loan.Id,
                ImportedByUserId = userId,
                SourceMismoFileId = sourceMismoFileId,
                MismoVersion = parsed.MismoVersion,
                ImportedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new MismoImportResult
            {
                LoanId = loan.Id,
                LoanNumber = loan.LoanNumber,
                CompanyNmlsNumber = companyNmlsNumber,
                MismoVersion = parsed.MismoVersion,
                ContentSha256 = contentSha256,
                IsDuplicate = false,
                BorrowerCount = parsed.Borrowers.Count,
                AssetCount = parsed.Assets.Count,
                LiabilityCount = parsed.Liabilities.Count,
                ImportedAtUtc = DateTime.UtcNow
            };
        }

        private static string ComputeSha256(byte[] bytes)
        {
            var hash = SHA256.HashData(bytes);
            var builder = new StringBuilder(hash.Length * 2);

            foreach (var item in hash)
                builder.Append(item.ToString("x2"));

            return builder.ToString();
        }

        private async Task<string> GenerateUniqueLoanNumberAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                var candidate = RandomNumberGenerator.GetInt32(0, 100000000).ToString("D8");

                var exists = await _context.Loans
                    .AnyAsync(x => x.LoanNumber == candidate, cancellationToken);

                if (!exists)
                    return candidate;
            }
        }
    }
}
