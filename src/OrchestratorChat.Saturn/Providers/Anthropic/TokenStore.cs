using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OrchestratorChat.Saturn.Providers.Anthropic;

/// <summary>
/// Interface for token storage operations to enable dependency injection and testing
/// </summary>
public interface ITokenStore
{
    /// <summary>
    /// Saves OAuth tokens securely
    /// </summary>
    /// <param name="tokens">The tokens to save</param>
    Task SaveTokensAsync(StoredTokens tokens);
    
    /// <summary>
    /// Stores OAuth tokens with individual parameters
    /// </summary>
    /// <param name="accessToken">The access token</param>
    /// <param name="refreshToken">The refresh token</param>
    /// <param name="expiresIn">Token expiration time in seconds</param>
    Task StoreTokensAsync(string accessToken, string refreshToken, int expiresIn);
    
    /// <summary>
    /// Loads OAuth tokens if they exist
    /// </summary>
    /// <returns>The stored tokens or null if none exist</returns>
    Task<StoredTokens?> LoadTokensAsync();
    
    /// <summary>
    /// Clears all stored OAuth tokens
    /// </summary>
    Task ClearTokensAsync();
    
    /// <summary>
    /// Gets whether the current tokens need to be refreshed
    /// </summary>
    bool NeedsRefresh { get; }
}

public class TokenStore : ITokenStore
{
    private readonly string _tokenPath;
    private readonly string _keyPath;
    private readonly string _saltPath;
    private StoredTokens? _cachedTokens;
    
    // Encryption parameters
    private const int KeySize = 256 / 8;  // 256-bit key
    private const int NonceSize = 12;     // 96-bit nonce for AES-GCM
    private const int TagSize = 16;       // 128-bit authentication tag
    private const int SaltSize = 16;      // 128-bit salt
    private const int Pbkdf2Iterations = 100000; // PBKDF2 iterations
    
    public TokenStore()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var authDir = Path.Combine(appDataPath, "OrchestratorChat", "auth");
        
        Directory.CreateDirectory(authDir);
        
        _tokenPath = Path.Combine(authDir, "anthropic.tokens");
        _keyPath = Path.Combine(authDir, ".keystore");
        _saltPath = Path.Combine(authDir, ".salt");
    }
    
    public async Task SaveTokensAsync(StoredTokens tokens)
    {
        var json = JsonSerializer.Serialize(tokens, new JsonSerializerOptions
        {
            WriteIndented = false
        });
        
        string encryptedData;
        if (IsWindows())
        {
            encryptedData = await EncryptWithDpapiAsync(json);
        }
        else
        {
            encryptedData = await EncryptWithAesAsync(json);
        }
        
        await File.WriteAllTextAsync(_tokenPath, encryptedData);
        _cachedTokens = tokens;
    }
    
    public async Task StoreTokensAsync(string accessToken, string refreshToken, int expiresIn)
    {
        var tokens = new StoredTokens
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn),
            CreatedAt = DateTime.UtcNow,
            TokenType = "Bearer",
            Scope = new[] { "user:profile", "user:inference" }
        };
        
        await SaveTokensAsync(tokens);
    }
    
    public async Task<StoredTokens?> LoadTokensAsync()
    {
        if (_cachedTokens != null)
        {
            return _cachedTokens;
        }
        
        if (!File.Exists(_tokenPath))
        {
            return null;
        }
        
        try
        {
            var encryptedData = await File.ReadAllTextAsync(_tokenPath);
            
            string decryptedJson;
            if (IsWindows())
            {
                decryptedJson = await DecryptWithDpapiAsync(encryptedData);
            }
            else
            {
                decryptedJson = await DecryptWithAesAsync(encryptedData);
            }
            
            _cachedTokens = JsonSerializer.Deserialize<StoredTokens>(decryptedJson);
            return _cachedTokens;
        }
        catch
        {
            // If decryption fails, try to migrate from legacy format
            _cachedTokens = await MigrateLegacyTokens();
            return _cachedTokens;
        }
    }
    
    public async Task ClearTokensAsync()
    {
        _cachedTokens = null;
        await Task.Run(() =>
        {
            SecureDeleteFile(_tokenPath);
            SecureDeleteFile(_keyPath);
            SecureDeleteFile(_saltPath);
        });
    }
    
    public bool NeedsRefresh
    {
        get
        {
            if (_cachedTokens == null)
            {
                return false;
            }
            return _cachedTokens.NeedsRefresh;
        }
    }
    
    private async Task<string> EncryptWithDpapiAsync(string plainText)
    {
        if (!IsWindows())
        {
            throw new PlatformNotSupportedException("DPAPI is only available on Windows");
        }
        
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        
        return await Task.Run(() =>
        {
            var protectedBytes = System.Security.Cryptography.ProtectedData.Protect(
                plainBytes,
                null, // No additional entropy
                System.Security.Cryptography.DataProtectionScope.CurrentUser);
            
            return Convert.ToBase64String(protectedBytes);
        });
    }
    
    private async Task<string> DecryptWithDpapiAsync(string encryptedData)
    {
        if (!IsWindows())
        {
            throw new PlatformNotSupportedException("DPAPI is only available on Windows");
        }
        
        var encryptedBytes = Convert.FromBase64String(encryptedData);
        
        return await Task.Run(() =>
        {
            var plainBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                encryptedBytes,
                null, // No additional entropy
                System.Security.Cryptography.DataProtectionScope.CurrentUser);
            
            return Encoding.UTF8.GetString(plainBytes);
        });
    }
    
    private async Task<string> EncryptWithAesAsync(string plainText)
    {
        var key = await GetOrCreateKeyAsync();
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        
        RandomNumberGenerator.Fill(nonce);
        
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        
        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);
        
        // Combine nonce + ciphertext + tag
        var result = new byte[NonceSize + cipherBytes.Length + TagSize];
        Array.Copy(nonce, 0, result, 0, NonceSize);
        Array.Copy(cipherBytes, 0, result, NonceSize, cipherBytes.Length);
        Array.Copy(tag, 0, result, NonceSize + cipherBytes.Length, TagSize);
        
        return Convert.ToBase64String(result);
    }
    
    private async Task<string> DecryptWithAesAsync(string encryptedData)
    {
        var key = await GetOrCreateKeyAsync();
        var data = Convert.FromBase64String(encryptedData);
        
        if (data.Length < NonceSize + TagSize)
        {
            throw new ArgumentException("Invalid encrypted data length");
        }
        
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherBytes = new byte[data.Length - NonceSize - TagSize];
        
        Array.Copy(data, 0, nonce, 0, NonceSize);
        Array.Copy(data, NonceSize, cipherBytes, 0, cipherBytes.Length);
        Array.Copy(data, NonceSize + cipherBytes.Length, tag, 0, TagSize);
        
        var plainBytes = new byte[cipherBytes.Length];
        
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
        
        return Encoding.UTF8.GetString(plainBytes);
    }
    
    private async Task<byte[]> GetOrCreateKeyAsync()
    {
        var salt = await GetOrCreateSaltAsync();
        
        // Use a machine-specific password for key derivation
        var password = Environment.MachineName + Environment.UserName;
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        
        using var pbkdf2 = new Rfc2898DeriveBytes(passwordBytes, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(KeySize);
    }
    
    private async Task<byte[]> GetOrCreateSaltAsync()
    {
        if (File.Exists(_saltPath))
        {
            return await File.ReadAllBytesAsync(_saltPath);
        }
        
        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);
        
        await File.WriteAllBytesAsync(_saltPath, salt);
        return salt;
    }
    
    private async Task<StoredTokens?> MigrateLegacyTokens()
    {
        // Check for legacy token files and attempt migration
        var legacyPaths = new[]
        {
            Path.Combine(Path.GetDirectoryName(_tokenPath)!, "tokens.json"),
            Path.Combine(Path.GetDirectoryName(_tokenPath)!, "anthropic_tokens.json")
        };
        
        foreach (var legacyPath in legacyPaths)
        {
            if (File.Exists(legacyPath))
            {
                try
                {
                    var legacyJson = await File.ReadAllTextAsync(legacyPath);
                    var tokens = JsonSerializer.Deserialize<StoredTokens>(legacyJson);
                    
                    if (tokens != null)
                    {
                        // Save in new encrypted format
                        await SaveTokensAsync(tokens);
                        
                        // Securely delete legacy file
                        await Task.Run(() => SecureDeleteFile(legacyPath));
                        
                        return tokens;
                    }
                }
                catch
                {
                    // Ignore migration failures
                }
            }
        }
        
        return null;
    }
    
    private static void SecureDeleteFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }
        
        try
        {
            // Overwrite file contents multiple times before deletion
            var fileInfo = new FileInfo(filePath);
            var length = fileInfo.Length;
            
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Write))
            {
                // Three passes of overwriting
                for (int pass = 0; pass < 3; pass++)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    
                    var buffer = new byte[1024];
                    var fillByte = pass switch
                    {
                        0 => (byte)0x00, // First pass: zeros
                        1 => (byte)0xFF, // Second pass: ones
                        _ => (byte)RandomNumberGenerator.GetInt32(256) // Third pass: random
                    };
                    
                    Array.Fill(buffer, fillByte);
                    
                    for (long written = 0; written < length; written += buffer.Length)
                    {
                        var toWrite = (int)Math.Min(buffer.Length, length - written);
                        stream.Write(buffer, 0, toWrite);
                    }
                    
                    stream.Flush();
                }
            }
        }
        catch
        {
            // If secure deletion fails, fall back to regular deletion
        }
        finally
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Ignore deletion failures
            }
        }
    }
    
    private static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}