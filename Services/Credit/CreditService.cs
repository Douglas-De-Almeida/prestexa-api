using PrestexaAPI.Data;
using PrestexaAPI.Models;
using PrestexaAPI.Models.Requests;
using PrestexaAPI.Models.Responses;
using PrestexaAPI.Services.Credit.Providers;

namespace PrestexaAPI.Services.Credit
{
    public class CreditService : ICreditService
    {
        private readonly ICreditRepository _repository;
        private readonly ICreditProviderAdapterFactory _providerFactory;
        private readonly AppDbContext _context;

        public CreditService(
            ICreditRepository repository,
            ICreditProviderAdapterFactory providerFactory,
            AppDbContext context)
        {
            _repository = repository;
            _providerFactory = providerFactory;
            _context = context;
        }

        public async Task<CreditReportResponse> OrderAsync(
            OrderCreditReportRequest request,
            CreditUserContext user,
            CancellationToken cancellationToken)
        {
            if (!request.ConsentAcknowledged)
                throw new InvalidOperationException("Borrower consent is required before ordering credit.");

            var loan = await _repository.FindLoanForAccessAsync(request.LoanNumber, user, cancellationToken);
            if (loan == null)
                throw new InvalidOperationException("Loan not found or not accessible.");

            var hasConsent = await _repository.HasConsentAsync(loan.Id, request.BorrowerId, request.CoBorrowerId, cancellationToken);
            if (!hasConsent)
                throw new InvalidOperationException("No borrower on this loan has authorized credit pull.");

            var provider = _providerFactory.Resolve(request.Provider);

            var providerResult = await provider.OrderAsync(new CreditProviderOrderRequest
            {
                LoanNumber = loan.LoanNumber,
                ReportType = request.ReportType,
                BorrowerId = request.BorrowerId,
                CoBorrowerId = request.CoBorrowerId,
                RequestedBy = user.Name
            }, cancellationToken);

            var report = new CreditReport
            {
                CompanyNmlsNumber = loan.CompanyNmlsNumber,
                LoanId = loan.Id,
                BorrowerId = request.BorrowerId,
                CoBorrowerId = request.CoBorrowerId,
                Provider = provider.ProviderKey,
                ReportType = request.ReportType,
                Status = providerResult.Status,
                OrderedByUserId = user.UserId,
                OrderedAtUtc = DateTime.UtcNow,
                TransUnionScore = providerResult.TransUnionScore,
                EquifaxScore = providerResult.EquifaxScore,
                ExperianScore = providerResult.ExperianScore,
                MiddleScore = providerResult.MiddleScore
            };

            await _repository.AddCreditReportAsync(report, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var tenantFolder = SanitizeFolderName(loan.CompanyNmlsNumber);
            var reportFolder = Path.Combine(
                "storage",
                "companies",
                tenantFolder,
                "loans",
                loan.LoanNumber,
                "credit",
                report.Id.ToString());

            Directory.CreateDirectory(reportFolder);

            var rawFileName = "raw.json";
            var xmlFileName = "report.xml";
            var pdfFileName = "report.pdf";

            var rawFullPath = Path.Combine(reportFolder, rawFileName);
            var xmlFullPath = Path.Combine(reportFolder, xmlFileName);
            var pdfFullPath = Path.Combine(reportFolder, pdfFileName);

            await File.WriteAllTextAsync(rawFullPath, providerResult.RawDataContent, cancellationToken);
            await File.WriteAllTextAsync(xmlFullPath, providerResult.XmlContent, cancellationToken);
            await File.WriteAllBytesAsync(pdfFullPath, providerResult.PdfBytes, cancellationToken);

            report.RawDataLocation = Path.Combine("companies", tenantFolder, "loans", loan.LoanNumber, "credit", report.Id.ToString(), rawFileName);
            report.XmlFileLocation = Path.Combine("companies", tenantFolder, "loans", loan.LoanNumber, "credit", report.Id.ToString(), xmlFileName);
            report.PdfFileLocation = Path.Combine("companies", tenantFolder, "loans", loan.LoanNumber, "credit", report.Id.ToString(), pdfFileName);

            var xmlDocument = new LoanDocument
            {
                CompanyNmlsNumber = loan.CompanyNmlsNumber,
                LoanId = loan.Id,
                UserId = user.UserId,
                Category = DocumentStorageCategory.Credit,
                FileName = $"credit-{report.Id}.xml",
                FilePath = report.XmlFileLocation,
                ContentType = "application/xml",
                FileSize = new FileInfo(xmlFullPath).Length,
                UploadedAt = DateTime.UtcNow
            };

            var pdfDocument = new LoanDocument
            {
                CompanyNmlsNumber = loan.CompanyNmlsNumber,
                LoanId = loan.Id,
                UserId = user.UserId,
                Category = DocumentStorageCategory.Credit,
                FileName = $"credit-{report.Id}.pdf",
                FilePath = report.PdfFileLocation,
                ContentType = "application/pdf",
                FileSize = new FileInfo(pdfFullPath).Length,
                UploadedAt = DateTime.UtcNow
            };

            _context.LoanDocuments.Add(xmlDocument);
            _context.LoanDocuments.Add(pdfDocument);

            var metadataJson =
                "{" +
                $"\"provider\":\"{provider.ProviderKey}\"," +
                $"\"scores\":{{\"tu\":{report.TransUnionScore?.ToString() ?? "null"},\"eq\":{report.EquifaxScore?.ToString() ?? "null"},\"ex\":{report.ExperianScore?.ToString() ?? "null"},\"middle\":{report.MiddleScore?.ToString() ?? "null"}}}" +
                "}";

            var activity = new LoanActivity
            {
                LoanId = loan.Id,
                LoanNumber = loan.LoanNumber,
                CompanyNmlsNumber = loan.CompanyNmlsNumber,
                ActivityType = LoanActivityType.SystemEvent,
                Message = $"Credit report ordered via {provider.ProviderKey}.",
                MetadataJson = metadataJson,
                NotifyLoanTeam = false,
                Visibility = LoanActivityVisibility.InternalOnly,
                ActorUserId = user.UserId,
                ActorName = user.Name,
                ActorRole = user.Role,
                ActorType = LoanActivityActorType.System,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.LoanActivities.Add(activity);
            await _repository.SaveChangesAsync(cancellationToken);

            return MapResponse(report, loan.LoanNumber);
        }

        public async Task<IReadOnlyList<CreditReportResponse>> GetReportsAsync(
            string loanId,
            CreditUserContext user,
            CancellationToken cancellationToken)
        {
            if (!int.TryParse(loanId, out var parsedLoanId))
                throw new InvalidOperationException("loanId must be a valid integer.");

            var loan = await _repository.FindLoanByIdForAccessAsync(parsedLoanId, user, cancellationToken);
            if (loan == null)
                throw new InvalidOperationException("Loan not found or not accessible.");

            var reports = await _repository.GetReportsForLoanAsync(loan.Id, cancellationToken);
            return reports.Select(r => MapResponse(r, loan.LoanNumber)).ToList();
        }

        private static CreditReportResponse MapResponse(CreditReport report, string loanNumber)
        {
            return new CreditReportResponse
            {
                ReportId = report.Id,
                LoanId = report.LoanId,
                LoanNumber = loanNumber,
                Provider = report.Provider,
                ReportType = report.ReportType.ToString(),
                Status = report.Status.ToString(),
                OrderedAtUtc = report.OrderedAtUtc,
                TransUnionScore = report.TransUnionScore,
                EquifaxScore = report.EquifaxScore,
                ExperianScore = report.ExperianScore,
                MiddleScore = report.MiddleScore,
                RawDataLocation = report.RawDataLocation,
                XmlFileLocation = report.XmlFileLocation,
                PdfFileLocation = report.PdfFileLocation
            };
        }

        private static string SanitizeFolderName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var filtered = value.Where(c => !invalid.Contains(c)).ToArray();
            return new string(filtered);
        }
    }
}
