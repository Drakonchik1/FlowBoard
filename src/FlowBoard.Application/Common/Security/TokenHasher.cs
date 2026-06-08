using System.Security.Cryptography;
using System.Text;

namespace FlowBoard.Application.Common.Security;

/// <summary>
/// Deterministic SHA-256 hashing for refresh tokens.
/// The plaintext token is sent to the client; only the hash is stored in the database.
/// </summary>
public static class TokenHasher
{
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
