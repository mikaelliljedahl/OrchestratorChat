using System.Security.Cryptography;
using System.Text;

namespace OrchestratorChat.Saturn.Providers.Anthropic;

/// <summary>
/// PKCE (Proof Key for Code Exchange) generator for OAuth 2.0
/// </summary>
public static class PKCEGenerator
{
    /// <summary>
    /// PKCE code verifier and challenge pair
    /// </summary>
    public class PKCEPair
    {
        public string Verifier { get; set; } = string.Empty;
        public string Challenge { get; set; } = string.Empty;
    }

    /// <summary>
    /// Generates a PKCE verifier and challenge pair
    /// </summary>
    /// <returns>PKCE pair with verifier and challenge</returns>
    public static PKCEPair Generate()
    {
        return Generate(128); // Use default length like SaturnFork
    }

    /// <summary>
    /// Generates a PKCE verifier and challenge pair with specified length
    /// </summary>
    /// <param name="verifierLength">Length of the verifier (43-128 characters)</param>
    /// <returns>PKCE pair with verifier and challenge</returns>
    public static PKCEPair Generate(int verifierLength)
    {
        // Validate verifier length according to RFC 7636
        if (verifierLength < 43 || verifierLength > 128)
            throw new ArgumentException("PKCE verifier length must be between 43 and 128 characters", nameof(verifierLength));

        // Generate random verifier using the same character set as SaturnFork
        var verifier = GenerateRandomString(verifierLength);

        // Create SHA256 hash as challenge
        var challengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(verifier));
        var challenge = Base64UrlEncode(challengeBytes);

        return new PKCEPair
        {
            Verifier = verifier,
            Challenge = challenge
        };
    }

    /// <summary>
    /// Generates a random string using safe characters for PKCE
    /// </summary>
    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
        var random = new byte[length];
        RandomNumberGenerator.Fill(random);
        
        var result = new StringBuilder(length);
        foreach (byte b in random)
        {
            result.Append(chars[b % chars.Length]);
        }
        
        return result.ToString();
    }

    /// <summary>
    /// Base64 URL-safe encoding without padding
    /// </summary>
    /// <param name="input">Bytes to encode</param>
    /// <returns>Base64 URL-safe encoded string</returns>
    private static string Base64UrlEncode(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}