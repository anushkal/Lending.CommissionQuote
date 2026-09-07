namespace CommissionQuote.Service.Contracts;

/// <summary>
/// Field-level validation for a <see cref="GenerateQuoteRequest"/>, per
/// contracts/commission-quotes-api.md and research.md #6 — the only outcome that does not reach
/// the vendor (FR-003). Extracted from <c>CommissionQuotesController</c> so the boundary cases
/// can be unit tested directly, without a `WebApplicationFactory` round-trip per case.
/// </summary>
public static class LoanDetailValidator
{
    /// <summary>Returns null when the request is valid; otherwise field name to error messages.</summary>
    public static Dictionary<string, string[]>? Validate(GenerateQuoteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, string[]>();

        if (request.LoanAmount <= 0 || request.LoanAmount > 100_000_000)
        {
            errors["loanAmount"] = ["Loan amount must be greater than 0 and no more than 100,000,000."];
        }

        if (request.LoanTermInMonths < 1 || request.LoanTermInMonths > 480)
        {
            errors["loanTermInMonths"] = ["Loan term must be a whole number of months between 1 and 480."];
        }

        if (request.RiskBand is not ("Low" or "Medium" or "High"))
        {
            errors["riskBand"] = ["Risk band must be one of Low, Medium, or High."];
        }

        return errors.Count == 0 ? null : errors;
    }
}
