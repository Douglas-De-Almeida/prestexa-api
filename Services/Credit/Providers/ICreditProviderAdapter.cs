namespace PrestexaAPI.Services.Credit.Providers
{
    public interface ICreditProviderAdapter
    {
        string ProviderKey { get; }
        Task<CreditProviderOrderResult> OrderAsync(CreditProviderOrderRequest request, CancellationToken cancellationToken);
    }
}
