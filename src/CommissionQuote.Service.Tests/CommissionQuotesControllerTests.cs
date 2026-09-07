using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommissionQuote.Service.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CommissionQuote.Service.Tests;

public class CommissionQuotesControllerTests(CommissionQuoteWebApplicationFactory factory)
    : IClassFixture<CommissionQuoteWebApplicationFactory>
{
    private static GenerateQuoteRequest ValidRequest => new()
    {
        LoanAmount = 250000,
        LoanTermInMonths = 180,
        RiskBand = "Medium",
    };

    private static HttpRequestMessage BuildRequest(GenerateQuoteRequest body, string? simulate = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/commission-quotes")
        {
            Content = JsonContent.Create(body),
        };

        if (simulate is not null)
        {
            request.Headers.Add("X-Vendor-Simulate", simulate);
        }

        return request;
    }

    [Fact]
    public async Task Post_ValidRequest_ForcedVendorSuccess_Returns200WithQuote()
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(BuildRequest(ValidRequest, "success"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var quote = await response.Content.ReadFromJsonAsync<CommissionQuoteResponse>();
        Assert.NotNull(quote);
        Assert.False(string.IsNullOrWhiteSpace(quote!.QuoteId));
        Assert.True(quote.CommissionRate > 0);
        Assert.True(quote.TotalCommission > 0);
    }

    [Fact]
    public async Task Post_VendorRejectsCredential_Returns500WithAuthFailureMessage()
    {
        // Simulates a misconfigured/invalid vendor credential (FR-012) by stripping the
        // api-key header the vendor client attaches, without touching the shared
        // VendorMock:ApiKey config the vendor mock's own filter checks against — so the two
        // sides genuinely disagree, the same way a real credential misconfiguration would.
        // 500 (not 502) because this is our own deployment's credential being wrong — an
        // internal fault, not the vendor being unhealthy.
        using var authFailureFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddHttpClient("VendorMock").AddHttpMessageHandler(() => new ApiKeyStrippingHandler())));
        var client = authFailureFactory.CreateClient();

        var response = await client.SendAsync(BuildRequest(ValidRequest));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(
            "Error connecting to the external commission quote API",
            await ReadMessageAsync(response));
    }

    [Fact]
    public async Task Post_ForcedVendorError_Returns502WithVendorErrorMessage()
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(BuildRequest(ValidRequest, "error"));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(
            "The commission quote service returned an error. Please try again.",
            await ReadMessageAsync(response));
    }

    [Fact]
    public async Task Post_VendorExceedsTimeout_Returns504WithTimeoutMessage()
    {
        // Overrides the configurable vendor-call timeout (VendorMock:RequestTimeoutMilliseconds)
        // to a small value and delays the vendor response past it, so this test verifies the
        // real 10-second timeout's behavior without actually waiting 10 seconds (Constitution
        // Principle II).
        using var timeoutFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["VendorMock:RequestTimeoutMilliseconds"] = "200",
                }));
            builder.ConfigureServices(services =>
                services.AddHttpClient("VendorMock").AddHttpMessageHandler(() => new DelayingHandler(TimeSpan.FromSeconds(1))));
        });
        var client = timeoutFactory.CreateClient();

        var response = await client.SendAsync(BuildRequest(ValidRequest, "success"));

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        Assert.Equal(
            "The commission quote service did not respond in time. Please try again.",
            await ReadMessageAsync(response));
    }

    [Theory]
    [InlineData(0, 180, "Medium", "loanAmount")]
    [InlineData(-1000, 180, "Medium", "loanAmount")]
    [InlineData(100_000_001, 180, "Medium", "loanAmount")]
    [InlineData(250000, 0, "Medium", "loanTermInMonths")]
    [InlineData(250000, 481, "Medium", "loanTermInMonths")]
    [InlineData(250000, 180, "Unknown", "riskBand")]
    public async Task Post_InvalidField_Returns400WithFieldError_AndNeverCallsVendor(
        decimal loanAmount, int loanTermInMonths, string riskBand, string invalidField)
    {
        var callCounter = new CallCountingHandler();
        using var countingFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddHttpClient("VendorMock").AddHttpMessageHandler(() => callCounter)));
        var client = countingFactory.CreateClient();

        var request = new GenerateQuoteRequest
        {
            LoanAmount = loanAmount,
            LoanTermInMonths = loanTermInMonths,
            RiskBand = riskBand,
        };

        var response = await client.SendAsync(BuildRequest(request));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty(invalidField, out _));
        Assert.Equal(0, callCounter.CallCount);
    }

    [Fact]
    public async Task Post_NonNumericLoanAmount_Returns400_AndNeverCallsVendor()
    {
        // A non-numeric loanAmount fails JSON model binding before LoanDetailValidator ever
        // runs (LoanAmount is a decimal) — a different boundary than the [Theory] cases above,
        // covered separately here.
        var callCounter = new CallCountingHandler();
        using var countingFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddHttpClient("VendorMock").AddHttpMessageHandler(() => callCounter)));
        var client = countingFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/commission-quotes")
        {
            Content = JsonContent.Create(new
            {
                loanAmount = "not-a-number",
                loanTermInMonths = 180,
                riskBand = "Medium",
            }),
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, callCounter.CallCount);
    }

    [Fact]
    public async Task Post_ValidRequest_ForcedVendorSuccess_LogsAtInformation()
    {
        var logProvider = new ListLoggerProvider();
        using var loggingFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logProvider)));
        var client = loggingFactory.CreateClient();

        await client.SendAsync(BuildRequest(ValidRequest, "success"));

        Assert.Contains(logProvider.Entries, e =>
            e.CategoryName.Contains("CommissionQuotesController") && e.Level == LogLevel.Information);
    }

    [Fact]
    public async Task Post_InvalidRequest_LogsAtWarning()
    {
        var logProvider = new ListLoggerProvider();
        using var loggingFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logProvider)));
        var client = loggingFactory.CreateClient();

        await client.SendAsync(BuildRequest(new GenerateQuoteRequest
        {
            LoanAmount = 0,
            LoanTermInMonths = 180,
            RiskBand = "Medium",
        }));

        Assert.Contains(logProvider.Entries, e =>
            e.CategoryName.Contains("CommissionQuotesController") && e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Post_VendorRejectsCredential_LogsAtWarning()
    {
        var logProvider = new ListLoggerProvider();
        using var authFailureFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(logProvider));
            builder.ConfigureServices(services =>
                services.AddHttpClient("VendorMock").AddHttpMessageHandler(() => new ApiKeyStrippingHandler()));
        });
        var client = authFailureFactory.CreateClient();

        await client.SendAsync(BuildRequest(ValidRequest));

        Assert.Contains(logProvider.Entries, e =>
            e.CategoryName.Contains("CommissionQuotesController") && e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Post_ForcedVendorError_LogsAtError()
    {
        var logProvider = new ListLoggerProvider();
        using var loggingFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logProvider)));
        var client = loggingFactory.CreateClient();

        await client.SendAsync(BuildRequest(ValidRequest, "error"));

        Assert.Contains(logProvider.Entries, e =>
            e.CategoryName.Contains("CommissionQuotesController") && e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task Post_VendorExceedsTimeout_LogsAtError()
    {
        var logProvider = new ListLoggerProvider();
        using var timeoutFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(logProvider));
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["VendorMock:RequestTimeoutMilliseconds"] = "200",
                }));
            builder.ConfigureServices(services =>
                services.AddHttpClient("VendorMock").AddHttpMessageHandler(() => new DelayingHandler(TimeSpan.FromSeconds(1))));
        });
        var client = timeoutFactory.CreateClient();

        await client.SendAsync(BuildRequest(ValidRequest, "success"));

        Assert.Contains(logProvider.Entries, e =>
            e.CategoryName.Contains("CommissionQuotesController") && e.Level == LogLevel.Error);
    }

    private static async Task<string?> ReadMessageAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("message").GetString();
    }

    private sealed class CallCountingHandler : DelegatingHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class ApiKeyStrippingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Remove("api-key");
            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class DelayingHandler(TimeSpan delay) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
