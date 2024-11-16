using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace AiLogAnalyzer.Core.Services;

public class CryptoService(IRegistryService registryService) : ICryptoService
{
    private byte[]? _entropy;

    public virtual string Encrypt(string plainText)
    {
        EnsureEntropyInitialized();
        using var aes = Aes.Create();
        aes.Key = _entropy;
        aes.GenerateIV();
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        var iv = aes.IV;
        var encrypted = ms.ToArray();

        var result = new byte[iv.Length + encrypted.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);

        return Convert.ToBase64String(result);
    }

    public virtual string Decrypt(string encryptedText)
    {
        EnsureEntropyInitialized();
        try
        {
            var fullCipher = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.Key = _entropy;
            aes.IV = iv;

            var cryptoTransform = aes.CreateDecryptor(aes.Key, aes.IV);

            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        catch
        {
            Log.Error("Failed to decrypt the API key. Returning null.");
            return null!;
        }
    }
    
    private void EnsureEntropyInitialized()
    {
        if (_entropy == null)
        {
            _entropy = CreateEntropy();
        }
    }

    protected virtual byte[] CreateEntropy()
    {
        try
        {
            var machineGuid = registryService.GetMachineGuid();
            if (string.IsNullOrEmpty(machineGuid))
            {
                throw new InvalidOperationException("Failed to retrieve machine GUID.");
            }

            var baseEntropy = "randomness-awaits-you" + machineGuid;
            var baseEntropyBytes = Encoding.UTF8.GetBytes(baseEntropy);
            return SHA256.HashData(baseEntropyBytes);
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred while creating entropy: {ex.Message}");
            throw;
        }
    }
}