using System.Security.Claims;

namespace PrestexaAPI.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? UserId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?
                    .User
                    .FindFirst("UserId")?
                    .Value;

                return int.TryParse(value, out var userId) ? userId : null;
            }
        }

        public string? CompanyNmlsNumber
        {
            get
            {
                return _httpContextAccessor.HttpContext?
                    .User
                    .FindFirst("CompanyNmlsNumber")?
                    .Value;
            }
        }

        public string? Role
        {
            get
            {
                return _httpContextAccessor.HttpContext?
                    .User
                    .FindFirst(ClaimTypes.Role)?
                    .Value;
            }
        }

        public bool IsSuperAdmin =>
            string.Equals(Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }
}