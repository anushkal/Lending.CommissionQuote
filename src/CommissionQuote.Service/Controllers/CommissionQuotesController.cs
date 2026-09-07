using CommissionQuote.Service.Contracts;
using CommissionQuote.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommissionQuote.Service.Controllers;

/// <summary>
/// The only endpoint the React frontend calls (contracts/commission-quotes-api.md). Never
/// exposes the vendor's api-key to the caller (Constitution Principle I, FR-011).
/// </summary>
[ApiController]
[Route("api/commission-quotes")]
public class CommissionQuotesController(
    IVendorQuoteClient vendorQuoteClient,
    ILogger<CommissionQuotesController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] GenerateQuoteRequest request, CancellationToken cancellationToken)
    {
        var errors = LoanDetailValidator.Validate(request);
        if (errors is not null)
        {
            logger.LogWarning(
                "Commission quote request rejected: validation failed for {Fields}",
                string.Join(", ", errors.Keys));
            return BadRequest(new { errors });
        }

        var outcome = await vendorQuoteClient.RequestQuoteAsync(request, cancellationToken);

        return HandleOutcome(outcome);
    }

    private IActionResult HandleOutcome(VendorQuoteOutcome outcome)
    {
        switch (outcome.Type)
        {
            case VendorQuoteOutcomeType.Success:
                logger.LogInformation("Commission quote {QuoteId} generated", outcome.Quote!.QuoteId);
                return Ok(new CommissionQuoteResponse
                {
                    QuoteId = outcome.Quote.QuoteId,
                    CommissionRate = outcome.Quote.CommissionRate,
                    TotalCommission = outcome.Quote.TotalCommission,
                });
            case VendorQuoteOutcomeType.AuthFailure:
                logger.LogWarning("Commission quote request rejected: vendor credential was not accepted");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error connecting to the external commission quote API" });
            case VendorQuoteOutcomeType.VendorError:
                logger.LogError("Commission quote request failed: vendor returned a simulated failure");
                return StatusCode(StatusCodes.Status502BadGateway,
                    new { message = "The commission quote service returned an error. Please try again." });
            case VendorQuoteOutcomeType.Timeout:
                logger.LogError("Commission quote request failed: vendor did not respond within the timeout");
                return StatusCode(StatusCodes.Status504GatewayTimeout,
                    new { message = "The commission quote service did not respond in time. Please try again." });
            default:
                logger.LogError("Commission quote request failed: unrecognized vendor outcome {OutcomeType}", outcome.Type);
                return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
