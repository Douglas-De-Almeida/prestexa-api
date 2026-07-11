namespace PrestexaAPI.Services.Credit.Providers
{
    public class CreditProviderAdapterFactory : ICreditProviderAdapterFactory
    {
        private readonly Dictionary<string, ICreditProviderAdapter> _adapters;

        public CreditProviderAdapterFactory(IEnumerable<ICreditProviderAdapter> adapters)
        {
            _adapters = adapters.ToDictionary(a => a.ProviderKey, StringComparer.OrdinalIgnoreCase);
        }

        public ICreditProviderAdapter Resolve(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
                throw new InvalidOperationException("Provider is required.");

            if (_adapters.TryGetValue(provider, out var adapter))
                return adapter;

            throw new InvalidOperationException($"Unsupported credit provider: {provider}.");
        }
    }
}
