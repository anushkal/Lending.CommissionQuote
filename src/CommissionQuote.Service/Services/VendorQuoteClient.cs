using System.Net;
using System.Net.Http.Json;
using CommissionQuote.Service.Contracts;
using CommissionQuote.Service.VendorSimulation;
using Microsoft.Extensions.Options;

namespace CommissionQuote.Service.Services;

/// <summary>
/// Calls the vendor mock over real HTTP (research.md #3) — the vendor lives in this same
/// process, so the request is sent back to the current request's own scheme/host rather than a
/// separately configured URL. Attaches the vendor api-key server-side only (Constitution
/// Principle I) and enforces a timeout (FR-013 default: 10s, configurable via
/// VendorMock:RequestTimeoutMilliseconds so tests can verify timeout handling quickly).
/// </summary>
public class VendorQuoteClient(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IOptions<VendorMockOptions> options) : IVendorQuoteClient
{
    private readonly VendorMockOptions _options = options.Value;

    public async Task<VendorQuoteOutcome> RequestQuoteAsync(GenerateQuoteRequest request, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("VendorMock");

        var vendorRequest = new VendorQuoteRequest
        {
            LoanAmount = request.LoanAmount,
            LoanTermInMonths = request.LoanTermInMonths,
            RiskBand = request.RiskBand,
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildVendorUri())
        {
            Content = JsonContent.Create(vendorRequest),
        };
        httpRequest.Headers.Add("api-key", _options.ApiKey);

        // Forwarded so tests can deterministically drive the public endpoint's outcome
        // (Constitution Principle II) without bypassing it to call the vendor mock directly.
        // Real frontend traffic never sends this header, so forwarding it is a no-op in
        // production.
        var currentHeaders = httpContextAccessor.HttpContext?.Request.Headers;
        if (currentHeaders is not null
            && currentHeaders.TryGetValue("X-Vendor-Simulate", out var simulateOverride)
            && !string.IsNullOrEmpty(simulateOverride))
        {
            httpRequest.Headers.Add("X-Vendor-Simulate", simulateOverride.ToString());
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(_options.RequestTimeoutMilliseconds));

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(httpRequest, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return VendorQuoteOutcome.Timeout();
        }
        catch (HttpRequestException)
        {
            return VendorQuoteOutcome.VendorError();
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return VendorQuoteOutcome.AuthFailure();
            }

            if (!response.IsSuccessStatusCode)
            {
                return VendorQuoteOutcome.VendorError();
            }

            var quote = await response.Content.ReadFromJsonAsync<VendorQuoteResponse>(cancellationToken: cancellationToken);
            return VendorQuoteOutcome.Success(quote!);
        }
    }

    private Uri BuildVendorUri()
    {
        var currentRequest = httpContextAccessor.HttpContext!.Request;
        return new Uri($"{currentRequest.Scheme}://{currentRequest.Host}/vendor/commission-quotes");
    }
}
