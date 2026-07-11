using System.Text.RegularExpressions;

namespace PrestexaAPI.Services
{
    public static class DomainValidationRules
    {
        public static readonly Regex SafeSubdomainRegex = new(
            "^[a-z0-9]+(?:-[a-z0-9]+)*$",
            RegexOptions.Compiled);

        public static readonly HashSet<string> ReservedSubdomains =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "admin",
                "api",
                "app",
                "support",
                "auth",
                "system",
                "www",
                "portal",
                "borrower",
                "rea"
            };

        public static bool IsReservedSubdomain(string subdomain)
        {
            return ReservedSubdomains.Contains(subdomain);
        }

        public static bool IsValidSubdomainFormat(string subdomain)
        {
            return SafeSubdomainRegex.IsMatch(subdomain);
        }
    }
}