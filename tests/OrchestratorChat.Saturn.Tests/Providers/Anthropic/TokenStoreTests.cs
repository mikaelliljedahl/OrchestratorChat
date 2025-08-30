using System.Collections.Concurrent;
using OrchestratorChat.Saturn.Providers.Anthropic;
using OrchestratorChat.Saturn.Tests.TestHelpers;

namespace OrchestratorChat.Saturn.Tests.Providers.Anthropic;

/// <summary>
/// Tests for TokenStore - Secure token storage with cross-platform encryption
/// </summary>
public class TokenStoreTests : IDisposable
{
    private readonly FileTestHelper _fileHelper;
    private readonly TestTokenStore _tokenStore;

    public TokenStoreTests()
    {
        _fileHelper = new FileTestHelper("TokenStoreTests");
        _tokenStore = new TestTokenStore(_fileHelper.TestDirectory);
    }

    [Fact]
    public async Task StoreTokensAsync_EncryptsAndStores()
    {
        // Arrange
        var tokens = CreateTestTokens();

        // Act
        await _tokenStore.SaveTokensAsync(tokens);

        // Assert
        Assert.True(_tokenStore.TokenFileExists());
        
        // Verify the file is encrypted (not plain text)
        var fileContent = _tokenStore.ReadTokenFileContent();
        Assert.DoesNotContain(tokens.AccessToken!, fileContent);
        Assert.DoesNotContain(tokens.RefreshToken!, fileContent);
        
        // Content should be base64 encoded (from encryption)
        Assert.True(IsBase64String(fileContent));
    }

    [Fact]
    public async Task GetTokensAsync_DecryptsAndReturns()
    {
        // Arrange
        var originalTokens = CreateTestTokens();
        await _tokenStore.SaveTokensAsync(originalTokens);

        // Act
        var retrievedTokens = await _tokenStore.LoadTokensAsync();

        // Assert
        Assert.NotNull(retrievedTokens);
        Assert.Equal(originalTokens.AccessToken, retrievedTokens.AccessToken);
        Assert.Equal(originalTokens.RefreshToken, retrievedTokens.RefreshToken);
        Assert.Equal(originalTokens.TokenType, retrievedTokens.TokenType);
        Assert.Equal(originalTokens.ExpiresAt, retrievedTokens.ExpiresAt);
        Assert.Equal(originalTokens.CreatedAt, retrievedTokens.CreatedAt);
        Assert.Equal(originalTokens.Scope, retrievedTokens.Scope);
    }

    [Fact]
    public async Task ClearTokensAsync_RemovesTokens()
    {
        // Arrange
        var tokens = CreateTestTokens();
        await _tokenStore.SaveTokensAsync(tokens);
        Assert.True(_tokenStore.TokenFileExists());

        // Act
        _tokenStore.DeleteTokens();

        // Assert
        Assert.False(_tokenStore.TokenFileExists());
        
        var retrievedTokens = await _tokenStore.LoadTokensAsync();
        Assert.Null(retrievedTokens);
    }

    [Fact]
    public void TokenExpiry_IsCheckedCorrectly()
    {
        // Arrange
        var validTokens = new StoredTokens
        {
            AccessToken = TestConstants.TestAccessToken,
            RefreshToken = TestConstants.TestRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };

        var expiredTokens = new StoredTokens
        {
            AccessToken = "expired_token",
            RefreshToken = "expired_refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };

        var nearExpiryTokens = new StoredTokens
        {
            AccessToken = "near_expiry_token",
            RefreshToken = "near_expiry_refresh",
            ExpiresAt = DateTime.UtcNow.AddMinutes(2), // Within 5 minute refresh window
            CreatedAt = DateTime.UtcNow.AddMinutes(-58),
            TokenType = "Bearer",
            Scope = new[] { "user:profile" }
        };

        // Assert
        Assert.False(validTokens.IsExpired);
        Assert.False(validTokens.NeedsRefresh);
        
        Assert.True(expiredTokens.IsExpired);
        Assert.True(expiredTokens.NeedsRefresh);
        
        Assert.False(nearExpiryTokens.IsExpired);
        Assert.True(nearExpiryTokens.NeedsRefresh);
    }

    [Fact]
    public async Task LoadTokensAsync_WithNoFile_ReturnsNull()
    {
        // Act
        var tokens = await _tokenStore.LoadTokensAsync();

        // Assert
        Assert.Null(tokens);
    }

    [Fact]
    public async Task LoadTokensAsync_WithCorruptedFile_ReturnsNull()
    {
        // Arrange
        _tokenStore.WriteCorruptedTokenFile();

        // Act
        var tokens = await _tokenStore.LoadTokensAsync();

        // Assert
        Assert.Null(tokens);
    }

    [Fact]
    public async Task SaveTokensAsync_OverwritesExistingTokens()
    {
        // Arrange
        var firstTokens = CreateTestTokens();
        await _tokenStore.SaveTokensAsync(firstTokens);

        var secondTokens = new StoredTokens
        {
            AccessToken = "new_access_token",
            RefreshToken = "new_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "different:scope" }
        };

        // Act
        await _tokenStore.SaveTokensAsync(secondTokens);

        // Assert
        var retrievedTokens = await _tokenStore.LoadTokensAsync();
        Assert.NotNull(retrievedTokens);
        Assert.Equal(secondTokens.AccessToken, retrievedTokens.AccessToken);
        Assert.Equal(secondTokens.RefreshToken, retrievedTokens.RefreshToken);
        Assert.NotEqual(firstTokens.AccessToken, retrievedTokens.AccessToken);
    }

    [Fact]
    public async Task EncryptionDecryption_IsConsistent()
    {
        // Arrange
        var tokens1 = CreateTestTokens();
        var tokens2 = CreateTestTokens();
        tokens2.AccessToken = "different_token";

        // Act
        await _tokenStore.SaveTokensAsync(tokens1);
        var retrieved1 = await _tokenStore.LoadTokensAsync();

        await _tokenStore.SaveTokensAsync(tokens2);
        var retrieved2 = await _tokenStore.LoadTokensAsync();

        // Assert
        Assert.NotNull(retrieved1);
        Assert.NotNull(retrieved2);
        Assert.Equal(tokens1.AccessToken, retrieved1.AccessToken);
        Assert.Equal(tokens2.AccessToken, retrieved2.AccessToken);
        Assert.NotEqual(retrieved1.AccessToken, retrieved2.AccessToken);
    }

    [Fact]
    public async Task SaveTokensAsync_WithNullValues_HandlesGracefully()
    {
        // Arrange
        var tokensWithNulls = new StoredTokens
        {
            AccessToken = TestConstants.TestAccessToken,
            RefreshToken = null, // null refresh token
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = null, // null token type
            Scope = null // null scope
        };

        // Act
        await _tokenStore.SaveTokensAsync(tokensWithNulls);
        var retrieved = await _tokenStore.LoadTokensAsync();

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(tokensWithNulls.AccessToken, retrieved.AccessToken);
        Assert.Null(retrieved.RefreshToken);
        Assert.Null(retrieved.TokenType);
        Assert.Null(retrieved.Scope);
    }

    [Fact]
    public async Task MultipleOperations_AreThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var results = new ConcurrentBag<StoredTokens?>();

        // Act - Perform concurrent operations
        for (int i = 0; i < 10; i++)
        {
            var i1 = i;
            tasks.Add(Task.Run(async () =>
            {
                var tokens = new StoredTokens
                {
                    AccessToken = $"token_{i1}",
                    RefreshToken = $"refresh_{i1}",
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    CreatedAt = DateTime.UtcNow,
                    TokenType = "Bearer"
                };
                
                var localTokenStore = new TestTokenStore(_fileHelper.CreateDirectory($"thread_{i1}"));
                await localTokenStore.SaveTokensAsync(tokens);
                var retrieved = await localTokenStore.LoadTokensAsync();
                results.Add(retrieved);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var resultsList = results.ToList();
        Assert.Equal(10, resultsList.Count);
        Assert.All(resultsList, r => Assert.NotNull(r));
        
        // Verify each thread got its own data
        for (int i = 0; i < 10; i++)
        {
            var expectedToken = $"token_{i}";
            Assert.Contains(resultsList, r => r!.AccessToken == expectedToken);
        }
    }

    [Fact]
    public async Task DeleteTokens_WithMultipleFiles_CleansUpAll()
    {
        // Arrange
        var tokens = CreateTestTokens();
        await _tokenStore.SaveTokensAsync(tokens);
        
        // Verify files exist
        Assert.True(_tokenStore.TokenFileExists());

        // Act
        _tokenStore.DeleteTokens();

        // Assert
        Assert.False(_tokenStore.TokenFileExists());
        Assert.False(_tokenStore.KeyFileExists());
        Assert.False(_tokenStore.SaltFileExists());
    }

    private static StoredTokens CreateTestTokens()
    {
        return new StoredTokens
        {
            AccessToken = TestConstants.TestAccessToken,
            RefreshToken = TestConstants.TestRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile", "user:inference", "org:create_api_key" }
        };
    }

    private static bool IsBase64String(string s)
    {
        try
        {
            Convert.FromBase64String(s);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _tokenStore.Dispose();
        _fileHelper.Dispose();
    }
}

/// <summary>
/// Test-specific TokenStore implementation that allows inspection of internal state
/// </summary>
public class TestTokenStore : TokenStore
{
    private readonly string _testDirectory;

    public TestTokenStore(string testDirectory)
    {
        _testDirectory = testDirectory;
        
        // Use reflection to set the internal paths to our test directory
        var tokenPathField = typeof(TokenStore).GetField("_tokenPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var keyPathField = typeof(TokenStore).GetField("_keyPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var saltPathField = typeof(TokenStore).GetField("_saltPath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        tokenPathField?.SetValue(this, Path.Combine(_testDirectory, "test.tokens"));
        keyPathField?.SetValue(this, Path.Combine(_testDirectory, ".testkeystore"));
        saltPathField?.SetValue(this, Path.Combine(_testDirectory, ".testsalt"));
    }

    public bool TokenFileExists()
    {
        return File.Exists(Path.Combine(_testDirectory, "test.tokens"));
    }

    public bool KeyFileExists()
    {
        return File.Exists(Path.Combine(_testDirectory, ".testkeystore"));
    }

    public bool SaltFileExists()
    {
        return File.Exists(Path.Combine(_testDirectory, ".testsalt"));
    }

    public string ReadTokenFileContent()
    {
        var tokenPath = Path.Combine(_testDirectory, "test.tokens");
        return File.ReadAllText(tokenPath);
    }

    public void WriteCorruptedTokenFile()
    {
        var tokenPath = Path.Combine(_testDirectory, "test.tokens");
        File.WriteAllText(tokenPath, "corrupted data that is not valid base64 or encrypted content!");
    }

    public void Dispose()
    {
        // Clean up test files
        try
        {
            var files = new[]
            {
                Path.Combine(_testDirectory, "test.tokens"),
                Path.Combine(_testDirectory, ".testkeystore"),
                Path.Combine(_testDirectory, ".testsalt")
            };

            foreach (var file in files.Where(File.Exists))
            {
                File.Delete(file);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}