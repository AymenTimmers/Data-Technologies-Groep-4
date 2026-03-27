using System.Security.Cryptography;
using System.Text;

namespace WebShop.Api;

public static class Security
{
    public static string HashPassword(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
