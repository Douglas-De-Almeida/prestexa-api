namespace PrestexaAPI.Services.Credit.Providers
{
    public interface ICreditProviderAdapterFactory
    {
        ICreditProviderAdapter Resolve(string provider);
    }
}
