using WebShop.Api.Helpers;

namespace WebShop.Api.Tests;

public class InputTests
{
    [Theory]
    [InlineData("USER@Example.com", "user@example.com")]
    [InlineData("  hello@world.org  ", "hello@world.org")]
    public void TryNormalizeEmail_ValidEmail_ReturnsNormalized(string input, string expected)
    {
        var ok = Input.TryNormalizeEmail(input, out var normalized);

        Assert.True(ok);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("   ")]
    public void TryNormalizeEmail_InvalidEmail_ReturnsFalse(string input)
    {
        var ok = Input.TryNormalizeEmail(input, out _);

        Assert.False(ok);
    }

    [Theory]
    [InlineData("12345", false)]
    [InlineData("123456", true)]
    [InlineData("password123", true)]
    public void IsValidPassword_EnforcesLength(string password, bool expected)
    {
        var valid = Input.IsValidPassword(password);

        Assert.Equal(expected, valid);
    }

    [Fact]
    public void NormalizeOptional_TrimAndCutToMaxLength()
    {
        var normalized = Input.NormalizeOptional("  abcdef  ", 4);

        Assert.Equal("abcd", normalized);
    }

    [Fact]
    public void HashPassword_SameInput_CanBeVerified()
    {
        var plainPassword = "password123";
        var hash = Security.HashPassword(plainPassword);

        // Bcrypt produces different hashes for the same password due to salt,
        // so verify the password against the hash instead of comparing values.
        var isVerified = Security.VerifyPassword(plainPassword, hash);

        Assert.True(isVerified);
        Assert.NotEmpty(hash);
    }
}