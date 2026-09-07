namespace CommissionQuote.Service.VendorSimulation;

public class VendorMockOptions
{
    public const string SectionName = "VendorMock";

    public string ApiKey { get; set; } = string.Empty;

    public double FailureRate { get; set; }

    /// <summary>
    /// How long the backend waits for the vendor call before treating it as a timeout
    /// (FR-013 default: 10000ms/10s). Overridable in tests so timeout behavior can be verified
    /// without a real 10-second wait (Constitution Principle II).
    /// </summary>
    public int RequestTimeoutMilliseconds { get; set; } = 10_000;
}
