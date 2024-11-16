namespace AiLogAnalyzer.Core.Services;

using Microsoft.Extensions.FileProviders;
using Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.IO.Abstractions;

public class ConfigurationService(
    IFileSystem fileSystem,
    ICryptoService? cryptoService = null,
    IFileProvider? fileProvider = null)
    : IConfigurationService
{
    private const string BaseConfigFileName = "appsettings";
    private const string BaseConfigFile = $"{BaseConfigFileName}.json";

    private const string UserSettingsFile = "userSettings.json";
    private static readonly string UserSettingsFilePath = Path.Combine(AppContext.BaseDirectory, UserSettingsFile);

    private const string EncryptionPrefix = "ENC:";

    private readonly string _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

    private AppConfig _cachedSettings;
    private IChangeToken _changeToken;
    private readonly ConfigurationBuilder _configurationBuilder = new();
    private readonly IFileProvider _fileProvider = fileProvider ?? new PhysicalFileProvider(AppContext.BaseDirectory);
    private readonly ICryptoService _cryptoService = cryptoService ?? new CryptoService(new RegistryService());

    public AppConfig LoadSettings(bool forceReload = false)
    {
        if (_cachedSettings != null && !forceReload && _changeToken is { HasChanged: false })
        {
            Log.Info("Returning cached settings.");
            return _cachedSettings;
        }

        try
        {
            Log.Info($"Trying to load settings for environment: {_environment}");

            var settings = new AppConfig();
            _configurationBuilder
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(_fileProvider, BaseConfigFile, optional: false, reloadOnChange: true)
                .AddJsonFile(_fileProvider, $"{BaseConfigFileName}.{_environment}.json", optional: true,
                    reloadOnChange: true);

            if (fileSystem.File.Exists(UserSettingsFilePath))
            {
                _configurationBuilder.AddJsonFile(_fileProvider, UserSettingsFile, optional: true,
                    reloadOnChange: true);
            }

            var config = _configurationBuilder.Build();
            config.Bind(settings);

            _changeToken = config.GetReloadToken();
            _changeToken.RegisterChangeCallback(OnSettingsChanged, null);

            if (_environment == "Production")
            {
                Log.Debug($"Settings before decryption: {JsonConvert.SerializeObject(settings, Formatting.Indented)}");
                ProcessApiKey(_cryptoService.Decrypt, settings);
                Log.Debug($"Settings after decryption: {JsonConvert.SerializeObject(settings, Formatting.Indented)}");

                var sanitizedSettingsString = GetSanitizedSettingsString(settings);

                Log.Info($"Settings loaded: {sanitizedSettingsString}");
            }
            else
            {
                Log.Info($"Settings loaded: {JsonConvert.SerializeObject(settings, Formatting.Indented)}");
            }

            _cachedSettings = settings;

            return settings;
        }
        catch (Exception ex)
        {
            Log.Error($"An exception occurred: {ex}");
            throw;
        }
    }

    public void SaveSettings(AppConfig settings)
    {
        Log.Debug($"Saving settings");
        try
        {
            string serializeSettingsString;
            if (_environment == "Production")
            {
                Log.Debug($"Settings before encryption: {JsonConvert.SerializeObject(settings, Formatting.Indented)}");
                ProcessApiKey(_cryptoService.Encrypt, settings);

                Log.Debug($"Settings after encryption: {JsonConvert.SerializeObject(settings, Formatting.Indented)}");
                serializeSettingsString = JsonConvert.SerializeObject(settings, Formatting.Indented);
                // var sanitizedSettingsString = GetSanitizedSettingsString(settings);
                // Log.Info($"Saving settings: {sanitizedSettingsString} to file {UserSettingsFilePath}");
            }
            else
            {
                serializeSettingsString = JsonConvert.SerializeObject(settings, Formatting.Indented);
                Log.Info($"Saving settings: {serializeSettingsString} to file {UserSettingsFilePath}");
            }

            fileSystem.File.WriteAllText(UserSettingsFilePath, serializeSettingsString);

            _cachedSettings = null;
        }
        catch (Exception ex)
        {
            Log.Error($"An exception occurred while saving settings: {ex}");
            throw;
        }
    }

    private void OnSettingsChanged(object state)
    {
        Log.Info("Configuration change detected, invalidating cache.");
        _cachedSettings = null;
    }

    private void ProcessApiKey(Func<string, string> keyProcessor, AppConfig settings)
    {
        try
        {
            var apiKey = settings.GeneralSettings.OpenAiSettings.ApiKey;

            if (string.IsNullOrEmpty(apiKey)) return;

            if (keyProcessor == _cryptoService.Decrypt && !apiKey.StartsWith(EncryptionPrefix)) return;
            if (keyProcessor == _cryptoService.Encrypt && apiKey.StartsWith(EncryptionPrefix)) return;

            var processedKey = keyProcessor(apiKey.Replace(EncryptionPrefix, string.Empty));
            settings.GeneralSettings.OpenAiSettings.ApiKey =
                keyProcessor == _cryptoService.Encrypt ? EncryptionPrefix + processedKey : processedKey;
        }
        catch (Exception ex)
        {
            Log.Error($"An exception occurred: {ex}");
            // Set API key to null or empty when decryption fails
            if (keyProcessor == _cryptoService.Decrypt)
            {
                settings.GeneralSettings.OpenAiSettings.ApiKey = null;
            }
        }
    }

    private static AppConfig SanitizeSettings(AppConfig settings)
    {
        var sanitizedSettings = JsonConvert.DeserializeObject<AppConfig>(JsonConvert.SerializeObject(settings));

        if (sanitizedSettings != null)
        {
            sanitizedSettings.GeneralSettings.OpenAiSettings.ApiKey = "****";
        }

        return sanitizedSettings;
    }

    private static string GetSanitizedSettingsString(AppConfig settings)
    {
        return JsonConvert.SerializeObject(SanitizeSettings(settings), Formatting.Indented);
    }
}