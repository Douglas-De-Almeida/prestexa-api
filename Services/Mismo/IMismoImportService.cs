using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PrestexaAPI.Services.Mismo
{
    public interface IMismoImportService
    {
        Task<MismoImportResult> ImportAsync(
            Stream xmlStream,
            int userId,
            string companyNmlsNumber,
            int? branchId = null,
            int? sourceMismoFileId = null,
            bool allowDuplicate = false,
            CancellationToken cancellationToken = default);
    }
}
