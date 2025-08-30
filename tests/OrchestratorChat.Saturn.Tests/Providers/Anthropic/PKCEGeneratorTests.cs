using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using OrchestratorChat.Saturn.Providers.Anthropic;

namespace OrchestratorChat.Saturn.Tests.Providers.Anthropic;

/// <summary>
/// Tests for PKCEGenerator - Proof Key for Code Exchange implementation
/// </summary>
public class PKCEGeneratorTests
{
    [Fact]
    public void GenerateCodeVerifier_MeetsRequirements()
    {
        // Act
        var pkcePair = PKCEGenerator.Generate();

        // Assert
        Assert.NotNull(pkcePair.Verifier);
        
        // RFC 7636 requirements for code verifier:
        // - Must be between 43-128 characters
        // - Must use unreserved characters: [A-Z] / [a-z] / [0-9] / "-" / "." / "_" / "~"
        Assert.True(pkcePair.Verifier.Length >= 43);
        Assert.True(pkcePair.Verifier.Length <= 128);
        
        // Base64URL encoding should only contain: A-Z, a-z, 0-9, -, _
        var base64UrlPattern = new Regex(@"^[A-Za-z0-9\-_]+$");
        Assert.True(base64UrlPattern.IsMatch(pkcePair.Verifier));
        
        // Should not contain padding characters
        Assert.DoesNotContain("=", pkcePair.Verifier);
        
        // Should not contain standard base64 characters that are URL-unsafe
        Assert.DoesNotContain("+", pkcePair.Verifier);
        Assert.DoesNotContain("/", pkcePair.Verifier);
    }

    [Fact]
    public void GenerateCodeChallenge_UsingSHA256_IsCorrect()
    {
        // Act
        var pkcePair = PKCEGenerator.Generate();

        // Assert
        Assert.NotNull(pkcePair.Challenge);
        
        // Manual verification: compute expected challenge from verifier
        var verifierBytes = Encoding.UTF8.GetBytes(pkcePair.Verifier);
        var challengeBytes = SHA256.HashData(verifierBytes);
        var expectedChallenge = Base64UrlEncode(challengeBytes);
        
        Assert.Equal(expectedChallenge, pkcePair.Challenge);
        
        // Challenge should be 43 characters for SHA256 (32 bytes -> 43 base64url chars)
        Assert.Equal(43, pkcePair.Challenge.Length);
        
        // Challenge should be base64url encoded
        var base64UrlPattern = new Regex(@"^[A-Za-z0-9\-_]+$");
        Assert.True(base64UrlPattern.IsMatch(pkcePair.Challenge));
        
        // Should not contain padding or unsafe characters
        Assert.DoesNotContain("=", pkcePair.Challenge);
        Assert.DoesNotContain("+", pkcePair.Challenge);
        Assert.DoesNotContain("/", pkcePair.Challenge);
    }

    [Fact]
    public void VerifierAndChallenge_AreConsistent()
    {
        // Arrange & Act - Generate multiple pairs
        var pairs = new List<PKCEGenerator.PKCEPair>();
        for (int i = 0; i < 10; i++)
        {
            pairs.Add(PKCEGenerator.Generate());
        }

        // Assert
        foreach (var pair in pairs)
        {
            // Each pair should be internally consistent
            var verifierBytes = Encoding.UTF8.GetBytes(pair.Verifier);
            var expectedChallengeBytes = SHA256.HashData(verifierBytes);
            var expectedChallenge = Base64UrlEncode(expectedChallengeBytes);
            
            Assert.Equal(expectedChallenge, pair.Challenge);
            
            // Each pair should be unique
            var otherPairs = pairs.Where(p => p != pair);
            Assert.All(otherPairs, otherPair => Assert.NotEqual(pair.Verifier, otherPair.Verifier));
            Assert.All(otherPairs, otherPair => Assert.NotEqual(pair.Challenge, otherPair.Challenge));
        }
    }

    [Fact]
    public void Generate_ProducesUniqueResults()
    {
        // Act - Generate multiple PKCE pairs
        var pairs = new List<PKCEGenerator.PKCEPair>();
        for (int i = 0; i < 100; i++)
        {
            pairs.Add(PKCEGenerator.Generate());
        }

        // Assert - All verifiers should be unique
        var verifiers = pairs.Select(p => p.Verifier).ToList();
        var uniqueVerifiers = verifiers.Distinct().ToList();
        Assert.Equal(verifiers.Count, uniqueVerifiers.Count);

        // Assert - All challenges should be unique
        var challenges = pairs.Select(p => p.Challenge).ToList();
        var uniqueChallenges = challenges.Distinct().ToList();
        Assert.Equal(challenges.Count, uniqueChallenges.Count);
    }

    [Fact]
    public void Generate_VerifierEntropy_IsSufficient()
    {
        // Act - Generate a PKCE pair
        var pkcePair = PKCEGenerator.Generate();

        // Assert - Check entropy characteristics
        var verifier = pkcePair.Verifier;
        
        // Should contain mix of character types (not all same character)
        var uniqueChars = verifier.Distinct().Count();
        Assert.True(uniqueChars > 10, "Verifier should have good character distribution");
        
        // Should not be predictable patterns
        Assert.False(verifier.StartsWith("AAAA") || verifier.StartsWith("1111"));
        Assert.False(verifier.EndsWith("AAAA") || verifier.EndsWith("1111"));
        
        // Should contain both letters and numbers/symbols (base64url alphabet)
        Assert.True(verifier.Any(char.IsLetter));
        Assert.True(verifier.Any(c => char.IsDigit(c) || c == '-' || c == '_'));
    }

    [Fact]
    public void Generate_ChallengeIsValidBase64Url()
    {
        // Act
        var pkcePair = PKCEGenerator.Generate();

        // Assert
        var challenge = pkcePair.Challenge;
        
        // Should be valid base64url - we can decode it
        try
        {
            var decoded = Base64UrlDecode(challenge);
            Assert.Equal(32, decoded.Length); // SHA256 produces 32 bytes
        }
        catch (Exception)
        {
            Assert.True(false, "Challenge should be valid base64url");
        }
    }

    [Fact]
    public void Generate_MultipleInvocations_AreThreadSafe()
    {
        // Arrange
        var results = new ConcurrentBag<PKCEGenerator.PKCEPair>();
        var tasks = new List<Task>();

        // Act - Generate pairs concurrently
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    results.Add(PKCEGenerator.Generate());
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var resultsList = results.ToList();
        Assert.Equal(500, resultsList.Count);

        // All should be unique
        var verifiers = resultsList.Select(r => r.Verifier).Distinct().ToList();
        Assert.Equal(500, verifiers.Count);

        // All should be valid
        foreach (var result in resultsList)
        {
            Assert.True(result.Verifier.Length >= 43);
            Assert.True(result.Challenge.Length == 43);
            
            // Verify consistency
            var verifierBytes = Encoding.UTF8.GetBytes(result.Verifier);
            var expectedChallenge = Base64UrlEncode(SHA256.HashData(verifierBytes));
            Assert.Equal(expectedChallenge, result.Challenge);
        }
    }

    [Fact]
    public void PKCEPair_Properties_AreNotNull()
    {
        // Act
        var pair = PKCEGenerator.Generate();

        // Assert
        Assert.NotNull(pair);
        Assert.NotNull(pair.Verifier);
        Assert.NotNull(pair.Challenge);
        Assert.NotEmpty(pair.Verifier);
        Assert.NotEmpty(pair.Challenge);
    }

    /// <summary>
    /// Helper method to encode bytes as base64url
    /// </summary>
    private static string Base64UrlEncode(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// Helper method to decode base64url string
    /// </summary>
    private static byte[] Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        
        // Add padding if needed
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }
        
        return Convert.FromBase64String(base64);
    }
}