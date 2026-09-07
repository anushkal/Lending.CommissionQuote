using CommissionQuote.Service.Contracts;
using Xunit;

namespace CommissionQuote.Service.Tests;

/// <summary>
/// Boundary-case coverage for <see cref="LoanDetailValidator"/> (research.md #6), independent of
/// the controller integration tests in <c>CommissionQuotesControllerTests</c> (T022), which cover
/// the HTTP-level contract rather than every edge of the validation logic itself.
/// </summary>
public class LoanDetailValidationTests
{
    private static GenerateQuoteRequest ValidRequest => new()
    {
        LoanAmount = 250000,
        LoanTermInMonths = 180,
        RiskBand = "Medium",
    };

    [Fact]
    public void Validate_ValidRequest_ReturnsNull()
    {
        Assert.Null(LoanDetailValidator.Validate(ValidRequest));
    }

    [Fact]
    public void Validate_NullRequest_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => LoanDetailValidator.Validate(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000.50)]
    public void Validate_LoanAmountZeroOrNegative_ReturnsLoanAmountError(decimal loanAmount)
    {
        var request = ValidRequest;
        request.LoanAmount = loanAmount;

        var errors = LoanDetailValidator.Validate(request);

        Assert.NotNull(errors);
        Assert.True(errors!.ContainsKey("loanAmount"));
    }

    [Theory]
    [InlineData(100_000_001)]
    [InlineData(999_999_999)]
    public void Validate_LoanAmountAboveMax_ReturnsLoanAmountError(decimal loanAmount)
    {
        var request = ValidRequest;
        request.LoanAmount = loanAmount;

        var errors = LoanDetailValidator.Validate(request);

        Assert.NotNull(errors);
        Assert.True(errors!.ContainsKey("loanAmount"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100_000_000)]
    public void Validate_LoanAmountAtBoundaries_IsValid(decimal loanAmount)
    {
        var request = ValidRequest;
        request.LoanAmount = loanAmount;

        Assert.Null(LoanDetailValidator.Validate(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_LoanTermZeroOrNegative_ReturnsLoanTermError(int loanTermInMonths)
    {
        var request = ValidRequest;
        request.LoanTermInMonths = loanTermInMonths;

        var errors = LoanDetailValidator.Validate(request);

        Assert.NotNull(errors);
        Assert.True(errors!.ContainsKey("loanTermInMonths"));
    }

    [Fact]
    public void Validate_LoanTermAboveMax_ReturnsLoanTermError()
    {
        var request = ValidRequest;
        request.LoanTermInMonths = 481;

        var errors = LoanDetailValidator.Validate(request);

        Assert.NotNull(errors);
        Assert.True(errors!.ContainsKey("loanTermInMonths"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(480)]
    public void Validate_LoanTermAtBoundaries_IsValid(int loanTermInMonths)
    {
        var request = ValidRequest;
        request.LoanTermInMonths = loanTermInMonths;

        Assert.Null(LoanDetailValidator.Validate(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("low")]
    [InlineData("Unknown")]
    [InlineData("Medium ")]
    public void Validate_UnrecognizedRiskBand_ReturnsRiskBandError(string riskBand)
    {
        var request = ValidRequest;
        request.RiskBand = riskBand;

        var errors = LoanDetailValidator.Validate(request);

        Assert.NotNull(errors);
        Assert.True(errors!.ContainsKey("riskBand"));
    }

    [Fact]
    public void Validate_AllFieldsInvalid_ReturnsAllThreeErrors()
    {
        var request = new GenerateQuoteRequest
        {
            LoanAmount = 0,
            LoanTermInMonths = 0,
            RiskBand = "Unknown",
        };

        var errors = LoanDetailValidator.Validate(request);

        Assert.NotNull(errors);
        Assert.Equal(3, errors!.Count);
        Assert.Contains("loanAmount", errors.Keys);
        Assert.Contains("loanTermInMonths", errors.Keys);
        Assert.Contains("riskBand", errors.Keys);
    }
}
