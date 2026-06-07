using WebShop.Api.Helpers;
using Xunit;

namespace WebShop.Api.Tests;

public class InputValidatorTests
{
    [Theory]
    [InlineData("valid@email.com", true)]
    [InlineData("user+tag@example.co.uk", true)]
    [InlineData("", false)]
    [InlineData("invalid.email", false)]
    [InlineData("invalid@", false)]
    [InlineData("@invalid.com", false)]
    [InlineData("test@test@test.com", false)]
    public void ValidateEmail_WithVaryingInputs_ReturnsCorrectly(string email, bool shouldBeValid)
    {
        var (isValid, errorMessage) = InputValidator.ValidateEmail(email);

        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.NotNull(errorMessage);
            Assert.Contains("email", errorMessage, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("ValidPass123!", true)]
    [InlineData("StrongPassword1!", true)]
    [InlineData("ABcd1234@", true)]
    [InlineData("a", false)] // Too short
    [InlineData("abc123", false)] // No uppercase
    [InlineData("ABC123", false)] // No lowercase
    [InlineData("abcdefgh", false)] // No digit
    [InlineData("Abcd1234", false)] // No special char
    [InlineData("VeryLongPasswordWith12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890", false)] // Too long
    public void ValidatePassword_WithVaryingInputs_ReturnsCorrectly(string password, bool shouldBeValid)
    {
        var (isValid, errorMessage) = InputValidator.ValidatePassword(password);

        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.NotNull(errorMessage);
            Assert.Contains("must", errorMessage, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("John", true)]
    [InlineData("Mary-Jane", true)]
    [InlineData("O'Brien", true)]
    [InlineData("Jean-Claude", true)]
    [InlineData(null, true)] // Optional
    [InlineData("", true)] // Optional
    [InlineData("   ", true)] // Optional whitespace
    [InlineData("John123", false)] // Contains digit
    [InlineData("John@Smith", false)] // Contains invalid char
    public void ValidateName_WithVaryingInputs_ReturnsCorrectly(string? name, bool shouldBeValid)
    {
        var (isValid, errorMessage) = InputValidator.ValidateName(name, "Test Name");

        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.NotNull(errorMessage);
        }
    }

    [Theory]
    [InlineData("123 Main Street", true)]
    [InlineData("123 Main St, Apt 4B", true)]
    [InlineData("P.O. Box 123", true)]
    [InlineData("123-A Main Road", true)]
    [InlineData(null, true)] // Optional
    [InlineData("", true)] // Optional
    [InlineData("123 Main St @ Invalid", false)] // Invalid character
    [InlineData("123 Main St; DROP TABLE", false)] // SQL injection attempt
    public void ValidateAddress_WithVaryingInputs_ReturnsCorrectly(string? address, bool shouldBeValid)
    {
        var (isValid, errorMessage) = InputValidator.ValidateAddress(address);

        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.NotNull(errorMessage);
        }
    }

    [Theory]
    [InlineData("DE89370400440532013000", true)]
    [InlineData("FR1420041010050500013M02606", true)]
    [InlineData("GB82WEST12345698765432", true)]
    [InlineData("IE64IRCE960105000715", true)]
    [InlineData("NL91ABNA0417164300", true)]
    [InlineData(null, true)] // Optional
    [InlineData("", true)] // Optional
    [InlineData("INVALID", false)] // Too short and invalid format
    [InlineData("123456789012345", false)] // No letters at start
    [InlineData("DE", false)] // Too short
    public void ValidateIBAN_WithVaryingInputs_ReturnsCorrectly(string? iban, bool shouldBeValid)
    {
        var (isValid, errorMessage) = InputValidator.ValidateIBAN(iban);

        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.NotNull(errorMessage);
        }
    }

    [Theory]
    [InlineData("2025-12-31", true)]
    [InlineData("2024-02-29", true)] // Leap year
    [InlineData("2023-02-28", true)]
    [InlineData(null, true)] // Optional
    [InlineData("", true)] // Optional
    [InlineData("2025-02-29", false)] // 2025 is not a leap year
    [InlineData("2024-13-01", false)] // Invalid month
    [InlineData("2024-04-31", false)] // April has 30 days
    [InlineData("2024-06-31", false)] // June has 30 days
    [InlineData("2024/12/31", false)] // Wrong format
    [InlineData("31-12-2024", false)] // Wrong format
    [InlineData("not-a-date", false)] // Invalid
    public void ValidateDate_WithVaryingInputs_ReturnsCorrectly(string? date, bool shouldBeValid)
    {
        var (isValid, errorMessage) = InputValidator.ValidateDate(date, "Test Date");

        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.NotNull(errorMessage);
        }
    }

    [Fact]
    public void ValidateDateOfBirth_WithFutureDate_ReturnsFalse()
    {
        var futureDate = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd");
        var (isValid, errorMessage) = InputValidator.ValidateDateOfBirth(futureDate);

        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("future", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateDateOfBirth_WithYoungAge_ReturnsFalse()
    {
        var youngDate = DateTime.UtcNow.AddYears(-15).ToString("yyyy-MM-dd");
        var (isValid, errorMessage) = InputValidator.ValidateDateOfBirth(youngDate);

        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("18", errorMessage);
    }

    [Fact]
    public void ValidateDateOfBirth_WithValidAdultAge_ReturnsTrue()
    {
        var adultDate = DateTime.UtcNow.AddYears(-25).ToString("yyyy-MM-dd");
        var (isValid, _) = InputValidator.ValidateDateOfBirth(adultDate);

        Assert.True(isValid);
    }

    [Fact]
    public void ValidateDateOfBirth_WithUnrealisticAge_ReturnsFalse()
    {
        var oldDate = DateTime.UtcNow.AddYears(-151).ToString("yyyy-MM-dd");
        var (isValid, errorMessage) = InputValidator.ValidateDateOfBirth(oldDate);

        Assert.False(isValid);
        Assert.NotNull(errorMessage);
        Assert.Contains("150", errorMessage);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void ValidateUserId_WithVaryingInputs_ReturnsCorrectly(int userId, bool shouldBeValid)
    {
        var (isValid, errorMessage) = InputValidator.ValidateUserId(userId);

        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.NotNull(errorMessage);
        }
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    public void ValidateQuantity_WithVaryingInputs_ReturnsCorrectly(int quantity, bool shouldBeValid)
    {
        var (isValid, errorMessage) = InputValidator.ValidateQuantity(quantity);

        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.NotNull(errorMessage);
        }
    }

    [Theory]
    [InlineData(1000)] // Exceeds max (999)
    public void ValidateQuantity_WithExceedsMax_ReturnsFalse(int quantity)
    {
        var (isValid, _) = InputValidator.ValidateQuantity(quantity);
        Assert.False(isValid);
    }

    [Theory]
    [InlineData(10.99, true)]
    [InlineData(0.01, true)]
    [InlineData(999999.99, true)]
    [InlineData(0, false)]
    [InlineData(-10.00, false)]
    [InlineData(10000000.00, false)] // Exceeds max
    public void ValidatePrice_WithVaryingInputs_ReturnsCorrectly(decimal price, bool shouldBeValid)
    {
        var (isValid, errorMessage) = InputValidator.ValidatePrice(price);

        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.NotNull(errorMessage);
        }
    }

    [Theory]
    [InlineData("laptop", true)]
    [InlineData("gaming laptop", true)]
    [InlineData("gaming-laptop", true)]
    [InlineData("", true)] // Optional
    [InlineData(null, true)] // Optional
    [InlineData("very very very very very very very very very very very very very very very very very very very very very very very very very very very long search query that exceeds limit", false)]
    [InlineData("laptop@home", false)] // Invalid character
    [InlineData("laptop; DROP TABLE", false)] // SQL injection attempt
    public void ValidateSearchQuery_WithVaryingInputs_ReturnsCorrectly(string? query, bool shouldBeValid)
    {
        var (isValid, errorMessage) = InputValidator.ValidateSearchQuery(query);

        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.NotNull(errorMessage);
        }
    }
}
