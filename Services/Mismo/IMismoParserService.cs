using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PrestexaAPI.Services.Mismo
{
    public interface IMismoParserService
    {
        Task<ParsedMismoLoanData> ParseAsync(Stream xmlStream, CancellationToken cancellationToken = default);
    }
}
