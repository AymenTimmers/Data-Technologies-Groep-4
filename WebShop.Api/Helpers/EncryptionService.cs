using System.Security.Cryptography;
using System.Text;

namespace WebShop.Api.Helpers;

/// <summary>
/// Service for encrypting and decrypting sensitive data using AES encryption.
/// </summary>
public class EncryptionService
{
    private readonly byte[] _encryptionKey;

    /// <summary>
    /// Initializes a new instance of the EncryptionService with a 256-bit encryption key.
    /// Key should be stored securely in environment variables or a secure key management system.
    /// </summary>
    public EncryptionService(string? encryptionKeyBase64 = null)
    {
        if (string.IsNullOrEmpty(encryptionKeyBase64))
        {
            encryptionKeyBase64 = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
        }

        if (string.IsNullOrEmpty(encryptionKeyBase64))
        {
            throw new InvalidOperationException("ENCRYPTION_KEY environment variable must be set or provided.");
        }

        _encryptionKey = Convert.FromBase64String(encryptionKeyBase64);

        if (_encryptionKey.Length != 32) // 256 bits
        {
            throw new InvalidOperationException("Encryption key must be 256 bits (32 bytes).");
        }
    }

    /// <summary>
    /// Generates a random 256-bit encryption key.
    /// Use this to initialize the ENCRYPTION_KEY environment variable.
    /// </summary>
    public static string GenerateEncryptionKey()
    {
        byte[] key = new byte[32]; // 256 bits
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key);
        }
        return Convert.ToBase64String(key);
    }

    /// <summary>
    /// Encrypts a plaintext string using AES-256-CBC.
    /// </summary>
    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return string.Empty;
        }

        using (var aes = Aes.Create())
        {
            aes.Key = _encryptionKey;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] iv = aes.IV;
            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream())
            {
                // Write IV to the beginning of the stream
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plaintext);
                }

                byte[] encryptedData = ms.ToArray();
                return Convert.ToBase64String(encryptedData);
            }
        }
    }

    /// <summary>
    /// Decrypts a base64-encoded encrypted string.
    /// </summary>
    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
        {
            return string.Empty;
        }

        try
        {
            byte[] buffer = Convert.FromBase64String(ciphertext);

            using (var aes = Aes.Create())
            {
                aes.Key = _encryptionKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Extract IV from the beginning of the buffer
                byte[] iv = new byte[aes.IV.Length];
                Array.Copy(buffer, 0, iv, 0, iv.Length);

                using (var decryptor = aes.CreateDecryptor(aes.Key, iv))
                using (var ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        catch
        {
            throw new InvalidOperationException("Failed to decrypt data. The ciphertext may be corrupted or the key may be incorrect.");
        }
    }
}
