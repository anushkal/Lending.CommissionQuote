using System.Net;
using System.Net.Http.Json;
using CommissionQuote.Service.VendorSimulation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CommissionQuote.Service.Tests;

/// <summary>
/// Exercises the vendor mock endpoint directly (contracts/vendor-mock-api.md), including the
/// constitution-mandated log level for each outcome — a lower-level complement to
/// <c>CommissionQuotesControllerTests</c>, which only reaches the vendor mock indirectly through
/// the staff-facing API.
/// </summary>
public class VendorSimulationTests(CommissionQuoteWebApplicationFactory factory)
    : IClassFixture<CommissionQuoteWebApplicationFactory>
{
    private const string ValidApiKey = "local-dev-vendor-key"; // matches appsettings.json VendorMock:ApiKey

    private static VendorQuoteRequest ValidRequest => new()
    {
        LoanAmount = 250000,
        LoanTermInMonths = 180,
        RiskBand = "Medium",
    };

    private static HttpRequestMessage BuildRequest(VendorQuoteRequest body, string? apiKey, string? simulate = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/vendor/commission-quotes")
        {
            Content = JsonContent.Create(body),
        };

        if (apiKey is not null)
        {
            request.Headers.Add("api-key", apiKey);
        }

        if (simulate is not null)
        {
            request.Headers.Add("X-Vendor-Simulate", simulate);
        }

        return request;
    }

    [Fact]
    public async Task Post_MissingApiKey_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(BuildRequest(ValidRequest, apiKey: null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidApiKey_Returns401()
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(BuildRequest(ValidRequest, apiKey: "wrong-key"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_MissingApiKey_LogsAtWarning()
    {
        var logProvider = new ListLoggerProvider();
        using var loggingFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logProvider)));
        var client = loggingFactory.CreateClient();

        await client.SendAsync(BuildRequest(ValidRequest, apiKey: null));

        Assert.Contains(logProvider.Entries, e =>
            e.CategoryName.Contains("VendorApiKeyAuthorizationFilter") && e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Post_ValidApiKey_ForcedSuccess_Returns200WithQuote()
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(BuildRequest(ValidRequest, ValidApiKey, "success"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var quote = await response.Content.ReadFromJsonAsync<VendorQuoteResponse>();
        Assert.NotNull(quote);
        Assert.False(string.IsNullOrWhiteSpace(quote!.QuoteId));
        Assert.True(quote.CommissionRate > 0);
    }

    [Fact]
    public async Task Post_ValidApiKey_ForcedSuccess_LogsAtInformation()
    {
        var logProvider = new ListLoggerProvider();
        using var loggingFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logProvider)));
        var client = loggingFactory.CreateClient();

        await client.SendAsync(BuildRequest(ValidRequest, ValidApiKey, "success"));

        Assert.Contains(logProvider.Entries, e =>
            e.CategoryName.Contains("VendorCommissionQuotesController") && e.Level == LogLevel.Information);
    }

    [Fact]
    public async Task Post_ValidApiKey_ForcedError_Returns500()
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(BuildRequest(ValidRequest, ValidApiKey, "error"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidApiKey_ForcedError_LogsAtError()
    {
        var logProvider = new ListLoggerProvider();
        using var loggingFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logProvider)));
        var client = loggingFactory.CreateClient();

        await client.SendAsync(BuildRequest(ValidRequest, ValidApiKey, "error"));

        Assert.Contains(logProvider.Entries, e =>
            e.CategoryName.Contains("VendorCommissionQuotesController") && e.Level == LogLevel.Error);
    }
}
