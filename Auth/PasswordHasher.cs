using System.Security.Cryptography;

namespace TournamentApi.Auth;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        
        var result = new byte[salt.Length + hash.Length];
        Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
        Buffer.BlockCopy(hash, 0, result, salt.Length, hash.Length);
        
        return Convert.ToBase64String(result);
    }

    public static bool Verify(string password, string passwordHash)
    {
        var bytes = Convert.FromBase64String(passwordHash);
        var salt = new byte[16];
        Buffer.BlockCopy(bytes, 0, salt, 0, 16);
        
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        
        for (int i = 0; i < hash.Length; i++)
        {
            if (bytes[i + 16] != hash[i])
                return false;
        }
        return true;
    }
}
