using CommissionQuote.Service.Services;
using CommissionQuote.Service.VendorSimulation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<VendorMockOptions>(
    builder.Configuration.GetSection(VendorMockOptions.SectionName));

// The vendor mock is reached over real HTTP from within this same process (research.md #3),
// not called as a direct method — so calling it needs an HttpClient and the current request's
// context (to call back into this app's own scheme/host).
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("VendorMock");
builder.Services.AddScoped<IVendorQuoteClient, VendorQuoteClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed so WebApplicationFactory<Program> can host this app in-process for integration tests.
public partial class Program;
