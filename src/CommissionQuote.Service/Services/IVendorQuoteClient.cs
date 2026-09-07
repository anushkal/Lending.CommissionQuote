using CommissionQuote.Service.Contracts;

namespace CommissionQuote.Service.Services;

public interface IVendorQuoteClient
{
    Task<VendorQuoteOutcome> RequestQuoteAsync(GenerateQuoteRequest request, CancellationToken cancellationToken);
}
