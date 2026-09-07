using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace CommissionQuote.Service.VendorSimulation;

/// <summary>
/// Runs as an authorization filter (before model binding parses the request body), per
/// Constitution Principle I: the api-key check must happen before any request body is parsed
/// or business logic runs.
/// </summary>
public class VendorApiKeyAuthorizationFilter(
    IOptions<VendorMockOptions> options,
    ILogger<VendorApiKeyAuthorizationFilter> logger) : IAsyncAuthorizationFilter
{
    private readonly VendorMockOptions _options = options.Value;

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var providedKey = context.HttpContext.Request.Headers["api-key"].ToString();

        if (string.IsNullOrEmpty(providedKey) || providedKey != _options.ApiKey)
        {
            logger.LogWarning("Vendor mock request rejected: missing or invalid api-key");
            context.Result = new UnauthorizedObjectResult(new { message = "Missing or invalid api-key" });
        }

        return Task.CompletedTask;
    }
}
