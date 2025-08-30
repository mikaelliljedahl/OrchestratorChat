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
        // Generate cryptographically secure random verifier (43-128 characters)
        var verifierBytes = new byte[32]; // 32 bytes = 43 base64url chars (without padding)
        RandomNumberGenerator.Fill(verifierBytes);
        var verifier = Base64UrlEncode(verifierBytes);

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