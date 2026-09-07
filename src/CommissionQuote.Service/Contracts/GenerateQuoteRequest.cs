namespace CommissionQuote.Service.Contracts;

public class GenerateQuoteRequest
{
    public decimal LoanAmount { get; set; }

    public int LoanTermInMonths { get; set; }

    public string RiskBand { get; set; } = string.Empty;
}
