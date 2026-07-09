using System.Globalization;
using System.Xml.Linq;

namespace PrestexaAPI.Services.Mismo
{
    public class MismoParserService : IMismoParserService
    {
        public async Task<ParsedMismoLoanData> ParseAsync(
            Stream xmlStream,
            CancellationToken cancellationToken = default)
        {
            var document = await XDocument.LoadAsync(xmlStream, LoadOptions.None, cancellationToken);
            var ns = document.Root?.Name.Namespace ?? XNamespace.None;

            var result = new ParsedMismoLoanData();

            result.MismoVersion = document
                .Descendants(ns + "DataVersionIdentifier")
                .Select(x => x.Value.Trim())
                .FirstOrDefault();

            var loanDetail = document.Descendants(ns + "LOAN_DETAIL").FirstOrDefault();
            var termsOfLoan = document.Descendants(ns + "TERMS_OF_LOAN").FirstOrDefault();
            var amortization = document.Descendants(ns + "AMORTIZATION").FirstOrDefault();

            if (loanDetail != null)
            {
                result.LoanTerms.LoanPurposeType = GetString(loanDetail, ns, "LoanPurposeType", "Purchase");
                result.LoanTerms.LienPriorityType = GetString(loanDetail, ns, "LienPriorityType", "FirstLien");
                result.LoanTerms.MortgageType = GetString(loanDetail, ns, "MortgageType", "Conventional");
                result.LoanTerms.NoteAmount = GetDecimal(loanDetail, ns, "NoteAmount");
            }

            if (termsOfLoan != null)
            {
                result.LoanTerms.NoteRatePercent = GetDecimal(termsOfLoan, ns, "NoteRatePercent");
                result.LoanTerms.LoanAmortizationPeriodCount = GetInt(termsOfLoan, ns, "LoanAmortizationPeriodCount", 360);
            }

            if (amortization != null)
            {
                result.LoanTerms.LoanAmortizationType = GetString(amortization, ns, "LoanAmortizationType", "Fixed");
            }

            var property = document.Descendants(ns + "PROPERTY").FirstOrDefault();

            if (property != null)
            {
                var address = property.Element(ns + "ADDRESS");
                var propertyDetail = property.Element(ns + "PROPERTY_DETAIL");

                if (address != null)
                {
                    result.SubjectProperty.AddressLineText = GetString(address, ns, "AddressLineText", string.Empty);
                    result.SubjectProperty.CityName = GetString(address, ns, "CityName", string.Empty);
                    result.SubjectProperty.StateCode = GetString(address, ns, "StateCode", string.Empty);
                    result.SubjectProperty.PostalCode = GetString(address, ns, "PostalCode", string.Empty);
                }

                if (propertyDetail != null)
                {
                    result.SubjectProperty.PropertyEstimatedValueAmount = GetDecimal(propertyDetail, ns, "PropertyEstimatedValueAmount");
                    result.SubjectProperty.PropertyUsageType = GetString(propertyDetail, ns, "PropertyUsageType", "PrimaryResidence");
                    result.SubjectProperty.FinancedUnitCount = GetInt(propertyDetail, ns, "FinancedUnitCount", 1);
                }
            }

            foreach (var party in document.Descendants(ns + "PARTY"))
            {
                var role = party
                    .Descendants(ns + "ROLE")
                    .Select(x => (string?)x.Attribute("RoleType"))
                    .FirstOrDefault(x =>
                        string.Equals(x, "Borrower", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x, "CoBorrower", StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrWhiteSpace(role))
                    continue;

                var individual = party.Element(ns + "INDIVIDUAL");
                var borrowerNode = party.Descendants(ns + "BORROWER").FirstOrDefault();
                var name = individual?.Element(ns + "NAME");

                var borrower = new ParsedMismoBorrower
                {
                    BorrowerRoleType = role,
                    FirstName = GetString(name, ns, "FirstName", string.Empty),
                    LastName = GetString(name, ns, "LastName", string.Empty),
                    EmailAddressText = party
                        .Descendants(ns + "EmailAddressText")
                        .Select(x => x.Value.Trim())
                        .FirstOrDefault()
                };

                var residence = borrowerNode?.Descendants(ns + "RESIDENCE").FirstOrDefault();

                if (residence != null)
                {
                    var residenceAddress = residence.Element(ns + "ADDRESS");
                    var residenceDetail = residence.Element(ns + "RESIDENCE_DETAIL");

                    borrower.CurrentAddress = new ParsedMismoBorrowerAddress
                    {
                        AddressLineText = GetString(residenceAddress, ns, "AddressLineText", string.Empty),
                        CityName = GetString(residenceAddress, ns, "CityName", string.Empty),
                        StateCode = GetString(residenceAddress, ns, "StateCode", string.Empty),
                        PostalCode = GetString(residenceAddress, ns, "PostalCode", string.Empty),
                        BorrowerResidencyType = GetString(residenceDetail, ns, "BorrowerResidencyType", "Current"),
                        ResidencyDurationMonthsCount = GetNullableInt(residenceDetail, ns, "ResidencyDurationMonthsCount")
                    };
                }

                foreach (var employer in borrowerNode?.Descendants(ns + "EMPLOYER") ?? Enumerable.Empty<XElement>())
                {
                    var employment = employer.Element(ns + "EMPLOYMENT");
                    if (employment == null)
                        continue;

                    var parsedEmployment = new ParsedMismoEmployment
                    {
                        EmployerName = employer
                            .Descendants(ns + "FullName")
                            .Select(x => x.Value.Trim())
                            .FirstOrDefault() ?? string.Empty,
                        EmploymentStatusType = GetString(employment, ns, "EmploymentStatusType", "Current"),
                        EmploymentStartDate = GetNullableDate(employment, ns, "EmploymentStartDate"),
                        EmploymentPositionDescription = GetString(employment, ns, "EmploymentPositionDescription", string.Empty)
                    };

                    borrower.Employments.Add(parsedEmployment);
                }

                foreach (var income in borrowerNode?.Descendants(ns + "CURRENT_INCOME_ITEM") ?? Enumerable.Empty<XElement>())
                {
                    borrower.Incomes.Add(new ParsedMismoIncome
                    {
                        IncomeType = GetString(income, ns, "IncomeType", "Base"),
                        CurrentIncomeMonthlyTotalAmount = GetDecimal(income, ns, "CurrentIncomeMonthlyTotalAmount")
                    });
                }

                result.Borrowers.Add(borrower);
            }

            foreach (var asset in document.Descendants(ns + "ASSET_DETAIL"))
            {
                result.Assets.Add(new ParsedMismoAsset
                {
                    AssetType = GetString(asset, ns, "AssetType", "Other"),
                    AssetCashOrMarketValueAmount = GetDecimal(asset, ns, "AssetCashOrMarketValueAmount")
                });
            }

            foreach (var liability in document.Descendants(ns + "LIABILITY_DETAIL"))
            {
                result.Liabilities.Add(new ParsedMismoLiability
                {
                    LiabilityType = GetString(liability, ns, "LiabilityType", "Other"),
                    MonthlyPaymentAmount = GetDecimal(liability, ns, "MonthlyPaymentAmount"),
                    UPBAmount = GetDecimal(liability, ns, "UPBAmount")
                });
            }

            return result;
        }

        private static string GetString(XElement? parent, XNamespace ns, string elementName, string fallback)
        {
            var value = parent?.Element(ns + elementName)?.Value?.Trim();
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static decimal GetDecimal(XElement? parent, XNamespace ns, string elementName)
        {
            var value = parent?.Element(ns + elementName)?.Value?.Trim();
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0m;
        }

        private static int GetInt(XElement? parent, XNamespace ns, string elementName, int fallback)
        {
            var value = parent?.Element(ns + elementName)?.Value?.Trim();
            return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        private static int? GetNullableInt(XElement? parent, XNamespace ns, string elementName)
        {
            var value = parent?.Element(ns + elementName)?.Value?.Trim();
            return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        private static DateTime? GetNullableDate(XElement? parent, XNamespace ns, string elementName)
        {
            var value = parent?.Element(ns + elementName)?.Value?.Trim();
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed.ToUniversalTime()
                : null;
        }
    }
}
