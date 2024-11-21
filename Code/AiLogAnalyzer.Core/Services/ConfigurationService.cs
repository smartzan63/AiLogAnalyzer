namespace AiLogAnalyzer.Core.Services;

using Microsoft.Extensions.FileProviders;
using Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.IO.Abstractions;

public class ConfigurationService(
    IFileSystem fileSystem,
    ICryptoService cryptoService,
    IFileProvider fileProvider)
    : IConfigurationService
{
    private const string BaseConfigFileName = "appsettings";
    private const string BaseConfigFile = $"{BaseConfigFileName}.json";

    private readonly string _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
    private const string EncryptionPrefix = "ENC:";

    private AppConfig? _cachedSettings;
    private IChangeToken? _changeToken;
    private readonly ConfigurationBuilder _configurationBuilder = new();
    private bool _isLoadingSettings;

    public AppConfig LoadSettings(bool forceReload = false)
    {
        if (_isLoadingSettings)
        {
            Log.Debug("LoadSettings already in progress. Skipping this invocation.");
            return _cachedSettings ?? new AppConfig();
        }

        if (_cachedSettings != null && !forceReload && _changeToken is { HasChanged: false })
        {
            Log.Info("Returning cached settings.");
            return _cachedSettings;
        }

        _isLoadingSettings = true;

        try
        {
            Log.Info($"Trying to load settings for environment: {_environment}");

            var baseConfigFilePath = Path.Combine(AppContext.BaseDirectory, BaseConfigFile);

            if (!fileSystem.File.Exists(baseConfigFilePath))
            {
                throw new FileNotFoundException($"The configuration file '{BaseConfigFile}' does not exist.");
            }

            var settings = new AppConfig();
            _configurationBuilder
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(fileProvider, BaseConfigFile, optional: false, reloadOnChange: true)
                .AddJsonFile(fileProvider, $"{BaseConfigFileName}.{_environment}.json", optional: true,
                    reloadOnChange: true);

            var config = _configurationBuilder.Build();
            config.Bind(settings);

            if (_changeToken == null || _changeToken.HasChanged)
            {
                _changeToken = config.GetReloadToken();
                _changeToken.RegisterChangeCallback(OnSettingsChanged, null);
            }

            SetDefaultPrompt(settings);

            if (_environment == "Production")
            {
                Log.Debug($"Settings before decryption: {JsonConvert.SerializeObject(settings, Formatting.Indented)}");
                ProcessApiKey(cryptoService.Decrypt, settings);
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
        finally
        {
            _isLoadingSettings = false;
        }
    }

    public void SaveSettings(AppConfig settings)
    {
        Log.Debug("Saving settings");
        try
        {
            var settingsFileName = $"{BaseConfigFileName}.{_environment}.json";
            var settingsFilePath = Path.Combine(AppContext.BaseDirectory, settingsFileName);

            SetDefaultPrompt(settings);

            string serializeSettingsString;
            if (_environment == "Production")
            {
                Log.Debug($"Settings before encryption: {JsonConvert.SerializeObject(settings, Formatting.Indented)}");
                ProcessApiKey(cryptoService.Encrypt, settings);

                Log.Debug($"Settings after encryption: {JsonConvert.SerializeObject(settings, Formatting.Indented)}");
                serializeSettingsString = JsonConvert.SerializeObject(settings, Formatting.Indented);
            }
            else
            {
                serializeSettingsString = JsonConvert.SerializeObject(settings, Formatting.Indented);
                Log.Info($"Saving settings: {serializeSettingsString} to file {settingsFilePath}");
            }

            fileSystem.File.WriteAllText(settingsFilePath, serializeSettingsString);

            _cachedSettings = settings; // Updating the cache at once
        }
        catch (Exception ex)
        {
            Log.Error($"An exception occurred while saving settings: {ex}");
            throw;
        }
    }

    private static void SetDefaultPrompt(AppConfig settings)
    {
        if (string.IsNullOrEmpty(settings.GeneralSettings.Prompt))
        {
            settings.GeneralSettings.Prompt = Prompts.WebViewLogAnalysisPrompt;
        }
    }

    private void OnSettingsChanged(object state)
    {
        Log.Info("Configuration change detected, invalidating cache.");
        _cachedSettings = null!;
    }

    private void ProcessApiKey(Func<string, string> keyProcessor, AppConfig settings)
    {
        try
        {
            var apiKey = settings.GeneralSettings.OpenAiSettings.ApiKey;

            if (string.IsNullOrEmpty(apiKey)) return;

            if (keyProcessor == cryptoService.Decrypt && !apiKey.StartsWith(EncryptionPrefix)) return;
            if (keyProcessor == cryptoService.Encrypt && apiKey.StartsWith(EncryptionPrefix)) return;

            var processedKey = keyProcessor(apiKey.Replace(EncryptionPrefix, string.Empty));
            settings.GeneralSettings.OpenAiSettings.ApiKey =
                keyProcessor == cryptoService.Encrypt ? EncryptionPrefix + processedKey : processedKey;
        }
        catch (Exception ex)
        {
            Log.Error($"An exception occurred: {ex}");
            // Set API key to null or empty when decryption fails
            if (keyProcessor == cryptoService.Decrypt)
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