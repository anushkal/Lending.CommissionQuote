namespace CommissionQuote.Service.Contracts;

public class CommissionQuoteResponse
{
    public string QuoteId { get; set; } = string.Empty;

    public decimal CommissionRate { get; set; }

    public decimal TotalCommission { get; set; }
}
