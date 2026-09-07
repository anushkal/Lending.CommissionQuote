using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CommissionQuote.Service.Tests;

/// <summary>
/// The vendor mock is reached over real HTTP from inside the same process (research.md #3).
/// In tests, that HTTP call needs to land back on this same in-memory TestServer rather than
/// a real socket — so the "VendorMock" named HttpClient's primary handler is replaced with the
/// TestServer's own handler.
/// </summary>
public class CommissionQuoteWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddHttpClient("VendorMock")
                .ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler());
        });
    }
}
