namespace CommissionQuote.Service.VendorSimulation;

public class VendorQuoteResponse
{
    public string QuoteId { get; set; } = string.Empty;

    public decimal CommissionRate { get; set; }

    public decimal TotalCommission { get; set; }
}
