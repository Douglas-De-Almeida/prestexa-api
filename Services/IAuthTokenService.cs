using PrestexaAPI.Models;

namespace PrestexaAPI.Services
{
    public interface IAuthTokenService
    {
        Task<string> CreateFinalJwtTokenAsync(User user, string portal);
    }
}