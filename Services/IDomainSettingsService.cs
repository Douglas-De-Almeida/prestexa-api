using PrestexaAPI.Models.Requests;
using PrestexaAPI.Models.Responses;

namespace PrestexaAPI.Services
{
    public interface IDomainSettingsService
    {
        Task<DomainSettingsResponse?> GetCurrentAsync(CancellationToken cancellationToken);

        Task<DomainSettingsResponse> UpsertCurrentAsync(UpdateDomainSettingsRequest request, CancellationToken cancellationToken);
    }
}
