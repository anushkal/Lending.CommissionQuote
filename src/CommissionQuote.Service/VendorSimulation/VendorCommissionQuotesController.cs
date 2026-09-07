using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CommissionQuote.Service.VendorSimulation;

/// <summary>
/// Simulates the external vendor's Commission Quote API (contracts/vendor-mock-api.md). Reached
/// only over real HTTP by CommissionQuote.Service's own vendor client, never called directly.
/// </summary>
[ApiController]
[Route("vendor/commission-quotes")]
[TypeFilter(typeof(VendorApiKeyAuthorizationFilter))]
public class VendorCommissionQuotesController(
    IOptions<VendorMockOptions> options,
    ILogger<VendorCommissionQuotesController> logger) : ControllerBase
{
    private readonly VendorMockOptions _options = options.Value;

    [HttpPost]
    public ActionResult<VendorQuoteResponse> Post(
        [FromBody] VendorQuoteRequest request,
        [FromHeader(Name = "X-Vendor-Simulate")] string? simulate)
    {
        if (ShouldSimulateFailure(simulate))
        {
            logger.LogError("Simulated vendor failure for a commission quote request");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Simulated vendor failure" });
        }

        var commissionRate = CalculateCommissionRate(request.RiskBand);
        var response = new VendorQuoteResponse
        {
            QuoteId = Guid.NewGuid().ToString(),
            CommissionRate = commissionRate,
            TotalCommission = Math.Round(request.LoanAmount * commissionRate, 2),
        };

        logger.LogInformation("Vendor mock generated quote {QuoteId}", response.QuoteId);
        return Ok(response);
    }

    private static decimal CalculateCommissionRate(string riskBand)
    {
        switch (riskBand)
        {
            case "Low":
                return 0.01m;
            case "Medium":
                return 0.02m;
            case "High":
                return 0.035m;
            default:
                return 0.02m;
        }
    }

    private bool ShouldSimulateFailure(string? simulate)
    {
        if (string.Equals(simulate, "success", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(simulate, "error", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Random.Shared.NextDouble() < _options.FailureRate;
    }
}
