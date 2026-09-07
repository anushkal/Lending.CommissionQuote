namespace CommissionQuote.Service.VendorSimulation;

public class VendorQuoteRequest
{
    public decimal LoanAmount { get; set; }

    public int LoanTermInMonths { get; set; }

    public string RiskBand { get; set; } = string.Empty;
}
