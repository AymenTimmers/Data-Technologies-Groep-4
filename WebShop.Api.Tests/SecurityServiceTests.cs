using WebShop.Api.Helpers;
using Xunit;

namespace WebShop.Api.Tests;

public class PasswordServiceTests
{
    [Fact]
    public void HashPassword_GeneratesDifferentHashesForSamePassword()
    {
        var password = "TestPassword123!";

        var hash1 = PasswordService.HashPassword(password);
        var hash2 = PasswordService.HashPassword(password);

        // Bcrypt should generate different hashes due to random salt
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var password = "TestPassword123!";
        var hash = PasswordService.HashPassword(password);

        var isValid = PasswordService.VerifyPassword(password, hash);

        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword456@";
        var hash = PasswordService.HashPassword(password);

        var isValid = PasswordService.VerifyPassword(wrongPassword, hash);

        Assert.False(isValid);
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ReturnsFalse()
    {
        var hash = PasswordService.HashPassword("TestPassword123!");

        var isValid = PasswordService.VerifyPassword("", hash);

        Assert.False(isValid);
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ReturnsFalse()
    {
        var isValid = PasswordService.VerifyPassword("TestPassword123!", "not-a-valid-hash");

        Assert.False(isValid);
    }

    [Fact]
    public void HashPassword_WithLongPassword_Works()
    {
        var longPassword = new string('a', 128) + "Bb1!";

        var hash = PasswordService.HashPassword(longPassword);

        Assert.NotEmpty(hash);
        Assert.True(PasswordService.VerifyPassword(longPassword, hash));
    }
}

public class EncryptionServiceTests
{
    private readonly string _testEncryptionKey = EncryptionService.GenerateEncryptionKey();

    [Fact]
    public void Encrypt_WithPlaintext_ReturnsEncryptedData()
    {
        var encryptionService = new EncryptionService(_testEncryptionKey);
        var plaintext = "sensitive@email.com";

        var encrypted = encryptionService.Encrypt(plaintext);

        Assert.NotEmpty(encrypted);
        Assert.NotEqual(plaintext, encrypted);
    }

    [Fact]
    public void Decrypt_WithEncryptedData_ReturnsOriginalPlaintext()
    {
        var encryptionService = new EncryptionService(_testEncryptionKey);
        var plaintext = "sensitive@email.com";

        var encrypted = encryptionService.Encrypt(plaintext);
        var decrypted = encryptionService.Decrypt(encrypted);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_WithEmptyString_ReturnsEmpty()
    {
        var encryptionService = new EncryptionService(_testEncryptionKey);

        var encrypted = encryptionService.Encrypt("");

        Assert.Empty(encrypted);
    }

    [Fact]
    public void Decrypt_WithEmptyString_ReturnsEmpty()
    {
        var encryptionService = new EncryptionService(_testEncryptionKey);

        var decrypted = encryptionService.Decrypt("");

        Assert.Empty(decrypted);
    }

    [Fact]
    public void EncryptDecrypt_WithSpecialCharacters_WorksCorrectly()
    {
        var encryptionService = new EncryptionService(_testEncryptionKey);
        var plaintext = "Test with special chars: !@#$%^&*() éàü 中文";

        var encrypted = encryptionService.Encrypt(plaintext);
        var decrypted = encryptionService.Decrypt(encrypted);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_WithLongText_WorksCorrectly()
    {
        var encryptionService = new EncryptionService(_testEncryptionKey);
        var plaintext = string.Concat(Enumerable.Repeat("test", 1000));

        var encrypted = encryptionService.Encrypt(plaintext);
        var decrypted = encryptionService.Decrypt(encrypted);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Decrypt_WithInvalidCiphertext_ThrowsException()
    {
        var encryptionService = new EncryptionService(_testEncryptionKey);

        Assert.Throws<InvalidOperationException>(() => encryptionService.Decrypt("invalid-base64!!!"));
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsException()
    {
        var service1 = new EncryptionService(_testEncryptionKey);
        var wrongKey = EncryptionService.GenerateEncryptionKey();
        var service2 = new EncryptionService(wrongKey);

        var plaintext = "sensitive data";
        var encrypted = service1.Encrypt(plaintext);

        Assert.Throws<InvalidOperationException>(() => service2.Decrypt(encrypted));
    }

    [Fact]
    public void GenerateEncryptionKey_ReturnsBase64String()
    {
        var key = EncryptionService.GenerateEncryptionKey();

        Assert.NotEmpty(key);
        // Should be valid base64 and decode to 32 bytes
        var decoded = Convert.FromBase64String(key);
        Assert.Equal(32, decoded.Length);
    }

    [Fact]
    public void EncryptionService_WithInvalidKeyLength_ThrowsException()
    {
        var invalidKey = Convert.ToBase64String(new byte[16]); // Only 128 bits instead of 256

        Assert.Throws<InvalidOperationException>(() => new EncryptionService(invalidKey));
    }

    [Fact]
    public void EncryptionService_WithMissingEnvironmentVariable_ThrowsException()
    {
        // Clear environment variable temporarily
        var originalValue = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
        Environment.SetEnvironmentVariable("ENCRYPTION_KEY", null);

        try
        {
            Assert.Throws<InvalidOperationException>(() => new EncryptionService(null));
        }
        finally
        {
            // Restore environment variable
            Environment.SetEnvironmentVariable("ENCRYPTION_KEY", originalValue);
        }
    }

    [Fact]
    public void MultipleEncrypts_WithSameKeyAndPlaintext_ProduceDifferentCiphertexts()
    {
        var encryptionService = new EncryptionService(_testEncryptionKey);
        var plaintext = "test data";

        var encrypted1 = encryptionService.Encrypt(plaintext);
        var encrypted2 = encryptionService.Encrypt(plaintext);

        // Different IVs should produce different ciphertexts
        Assert.NotEqual(encrypted1, encrypted2);
        // But both should decrypt to the same plaintext
        Assert.Equal(plaintext, encryptionService.Decrypt(encrypted1));
        Assert.Equal(plaintext, encryptionService.Decrypt(encrypted2));
    }
}
