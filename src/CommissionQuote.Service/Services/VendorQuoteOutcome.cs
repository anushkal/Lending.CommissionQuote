using CommissionQuote.Service.VendorSimulation;

namespace CommissionQuote.Service.Services;

public enum VendorQuoteOutcomeType
{
    Success,
    AuthFailure,
    VendorError,
    Timeout,
}

/// <summary>
/// The result of calling the vendor mock, already classified into the failure modes
/// CommissionQuotesController needs to distinguish (contracts/commission-quotes-api.md).
/// </summary>
public class VendorQuoteOutcome
{
    public required VendorQuoteOutcomeType Type { get; init; }

    public VendorQuoteResponse? Quote { get; init; }

    public static VendorQuoteOutcome Success(VendorQuoteResponse quote) =>
        new() { Type = VendorQuoteOutcomeType.Success, Quote = quote };

    public static VendorQuoteOutcome AuthFailure() => new() { Type = VendorQuoteOutcomeType.AuthFailure };

    public static VendorQuoteOutcome VendorError() => new() { Type = VendorQuoteOutcomeType.VendorError };

    public static VendorQuoteOutcome Timeout() => new() { Type = VendorQuoteOutcomeType.Timeout };
}
