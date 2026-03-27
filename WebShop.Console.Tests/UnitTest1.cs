namespace WebShop.Console.Tests;

public class ConsoleInputValidationTests
{
    [Theory]
    [InlineData("USER@Example.com", "user@example.com")]
    [InlineData("  test@site.net ", "test@site.net")]
    public void TryNormalizeEmail_Valid_ReturnsNormalized(string input, string expected)
    {
        var ok = ConsoleInputValidation.TryNormalizeEmail(input, out var normalized);

        Assert.True(ok);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("   ")]
    public void TryNormalizeEmail_Invalid_ReturnsFalse(string input)
    {
        var ok = ConsoleInputValidation.TryNormalizeEmail(input, out _);

        Assert.False(ok);
    }

    [Fact]
    public void PasswordValidation_RespectsMinMax()
    {
        Assert.False(ConsoleInputValidation.IsValidPassword("12345", 6, 128));
        Assert.True(ConsoleInputValidation.IsValidPassword("123456", 6, 128));
    }

    [Fact]
    public void PositiveLongParsing_Works()
    {
        var ok = ConsoleInputValidation.TryParsePositiveLong("42", out var value);

        Assert.True(ok);
        Assert.Equal(42, value);
    }

    [Fact]
    public void RangeParsing_RejectsOutOfRange()
    {
        var ok = ConsoleInputValidation.TryParseIntInRange("101", 1, 100, out _);

        Assert.False(ok);
    }

    [Fact]
    public void OptionalTextValidation_AllowsEmptyAndRejectsLong()
    {
        Assert.True(ConsoleInputValidation.IsValidOptionalText("", 40));
        Assert.False(ConsoleInputValidation.IsValidOptionalText(new string('x', 41), 40));
    }
}