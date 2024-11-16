namespace Unit.Tests;

using System.IO.Abstractions;
using AiLogAnalyzer.Core.Configuration;
using AiLogAnalyzer.Core.Services;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;

[TestClass]
public class ConfigurationServiceTests
{
    private ConfigurationService _configurationService;
    private Mock<IFileSystem> _mockFileSystem;
    private Mock<CryptoService> _mockCryptoService;
    private InMemoryFileProvider _fileProvider;
    private readonly string _userSettingsFilePath = "userSettings.json";
    private readonly string _environment = "Development";

    [TestInitialize]
    public void Setup()
    {
        // Mock the file system and crypto service
        _mockFileSystem = new Mock<IFileSystem>();
        _mockCryptoService = new Mock<CryptoService>(new RegistryService());

        // Set up the File.Exists method
        _mockFileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);

        // Create in-memory configuration files
        var files = new Dictionary<string, string>
        {
            { "appsettings.json", "{}" },
            { $"appsettings.{_environment}.json", "{}" },
            { "userSettings.json", $"{{ \"GeneralSettings\": {{ \"OpenAiSettings\": {{ \"ApiKey\": \"test-api-key\" }} }} }}" }
        };

        _fileProvider = new InMemoryFileProvider(files);

        // Create the configuration service with mocks
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _environment);
        _configurationService = new ConfigurationService(_mockFileSystem.Object, _mockCryptoService.Object, _fileProvider);
    }
    
    [TestMethod]
    public void SaveSettings_ShouldEncryptApiKey_WhenEnvironmentIsProduction()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var appConfig = GetAppConfig();
        const string encryptedApiKey = "encrypted-api-key";
        
        string writtenContent = null;
        _mockFileSystem.Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string path, string content) => writtenContent = content);

        // Mock the decryption
        var mockCryptoService = new Mock<ICryptoService>();
        mockCryptoService.Setup(cs => cs.Encrypt(It.IsAny<string>())).Returns(encryptedApiKey);

        // Create a new instance of ConfigurationService with the production environment
        var configurationService = new ConfigurationService(_mockFileSystem.Object, mockCryptoService.Object );

        // Act
        configurationService.SaveSettings(appConfig);

        // Assert
        writtenContent.Should().NotBeNull("Settings should have been written to the file.");
        writtenContent.Should().Contain("ENC:", "The API key should be encrypted in non-production environments.");
    }

    [TestMethod]
    public void SaveSettings_ShouldNotEncryptApiKey_WhenEnvironmentIsNotProduction()
    {
        // Arrange
        var appConfig = GetAppConfig();

        // Mock the File.WriteAllText method to capture the written content
        string writtenContent = null;
        _mockFileSystem.Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Callback((string path, string content) => writtenContent = content);

        // Act
        _configurationService.SaveSettings(appConfig);

        // Assert
        writtenContent.Should().NotBeNull("Settings should have been written to the file.");
        writtenContent.Should().NotContain("ENC:", "The API key should not be encrypted in non-production environments.");
        writtenContent.Should().Contain("test-api-key", "The API key should be saved as plain text in non-production environments.");
    }
    
    [TestMethod]
    public void SaveSettings_ShouldNotReEncryptApiKey_WhenApiKeyIsAlreadyEncrypted()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var appConfig = GetAppConfig();
        appConfig.GeneralSettings.OpenAiSettings.ApiKey = "ENC:already-encrypted-key";

        // Mock the crypto service to avoid actual encryption
        _mockCryptoService.Setup(cs => cs.Encrypt(It.IsAny<string>())).Returns("ENC:already-encrypted-key");

        // Act
        _configurationService.SaveSettings(appConfig);
        _mockFileSystem.Setup(fs => fs.File.ReadAllText(It.IsAny<string>())).Returns(JsonConvert.SerializeObject(appConfig));

        var savedSettings = _mockFileSystem.Object.File.ReadAllText(_userSettingsFilePath);

        // Assert
        savedSettings.Should().Contain("ENC:already-encrypted-key", "The API key should not be re-encrypted if it is already encrypted.");
        savedSettings.Should().NotContain("ENC:ENC:", "The API key should not be encrypted multiple times.");
    }

    [TestMethod]
    public void LoadSettings_ShouldDecryptApiKey_WhenEnvironmentIsProduction()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var appConfig = GetAppConfig();
        var encryptedApiKey = "ENC:encrypted-api-key";

        // Mock the decryption
        var mockCryptoService = new Mock<ICryptoService>();
        mockCryptoService.Setup(cs => cs.Decrypt(It.IsAny<string>())).Returns("decrypted-api-key");

        // Update the in-memory files
        var files = new Dictionary<string, string>
        {
            { "appsettings.json", "{}" },
            { $"appsettings.Production.json", "{}" },
            { "userSettings.json", $"{{ \"GeneralSettings\": {{ \"OpenAiSettings\": {{ \"ApiKey\": \"{encryptedApiKey}\" }} }} }}" }
        };

        var inMemoryFileProvider = new InMemoryFileProvider(files);

        // Create a new instance of ConfigurationService with the production environment
        var configurationService = new ConfigurationService(_mockFileSystem.Object, mockCryptoService.Object, inMemoryFileProvider );

        // Act
        var loadedConfig = configurationService.LoadSettings(true);

        // Assert
        loadedConfig.GeneralSettings.OpenAiSettings.ApiKey.Should().Be("decrypted-api-key", "The API key must be decrypted after loading.");
    }
    
    [TestMethod]
    public void LoadSettings_ShouldReturnCachedSettings_WhenSettingsHaveNotChanged()
    {
        // Arrange
        var appConfig = GetAppConfig();
        _configurationService.SaveSettings(appConfig);

        // Act
        var loadedConfig1 = _configurationService.LoadSettings();
        var loadedConfig2 = _configurationService.LoadSettings();

        // Assert
        loadedConfig2.Should().BeSameAs(loadedConfig1, "Settings should be cached and the same instance should be returned.");
    }
    
    [TestMethod]
    public void LoadSettings_ShouldReloadSettings_WhenForceReloadIsTrue()
    {
        // Arrange
        var appConfig = GetAppConfig();
        _configurationService.SaveSettings(appConfig);

        var loadedConfig1 = _configurationService.LoadSettings();

        // Act
        var loadedConfig2 = _configurationService.LoadSettings(forceReload: true);

        // Assert
        loadedConfig2.Should().NotBeSameAs(loadedConfig1, "Settings should be reloaded when forceReload is true.");
    }
    
    [TestMethod]
    public void LoadSettings_ShouldHandleInvalidEncryptedApiKey_Gracefully()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        var appConfig = GetAppConfig();
        appConfig.GeneralSettings.OpenAiSettings.ApiKey = "ENC:invalid-encrypted-value";

        var mockCryptoService = new Mock<ICryptoService>();
        mockCryptoService.Setup(cs => cs.Decrypt(It.IsAny<string>())).Throws<Exception>();

        // Update the in-memory files
        var files = new Dictionary<string, string>
        {
            { "appsettings.json", "{}" },
            { "userSettings.json", $"{{ \"GeneralSettings\": {{ \"OpenAiSettings\": {{ \"ApiKey\": \"ENC:invalid-encrypted-value\" }} }} }}" }
        };

        var inMemoryFileProvider = new InMemoryFileProvider(files);

        var configurationService = new ConfigurationService(_mockFileSystem.Object, mockCryptoService.Object, inMemoryFileProvider);

        // Act
        var loadedConfig = configurationService.LoadSettings(true);

        // Assert
        loadedConfig.GeneralSettings.OpenAiSettings.ApiKey
            .Should().BeNullOrEmpty("Invalid encrypted API key should result in null or empty API key after failed decryption.");
    }
    
    [TestMethod]
    public void LoadSettings_ShouldReloadSettings_WhenSettingsFileChanges()
    {
        // Arrange
        var appConfig = GetAppConfig();
        
        var mockCryptoService = new Mock<ICryptoService>();
        // Update the in-memory files
        var files = new Dictionary<string, string>
        {
            { "appsettings.json", "{}" },
            { $"appsettings.Production.json", "{}" },
            { "userSettings.json", $"{{ \"GeneralSettings\": {{ \"OpenAiSettings\": {{ \"ApiKey\": \"modified-api-key\" }} }} }}" }
        };
    
        var inMemoryFileProvider = new InMemoryFileProvider(files);
    
        // Create a new instance of ConfigurationService with the production environment
        var configurationService = new ConfigurationService(_mockFileSystem.Object, mockCryptoService.Object, inMemoryFileProvider);
        
        configurationService.SaveSettings(appConfig);
    
        var loadedConfig1 = configurationService.LoadSettings();
    
        // Modify the settings file directly
        appConfig.GeneralSettings.OpenAiSettings.ApiKey = "modified-api-key";
        configurationService.SaveSettings(appConfig);
    
        // Force reload
        var loadedConfig2 = configurationService.LoadSettings(true);
    
        // Assert
        loadedConfig2.GeneralSettings.OpenAiSettings.ApiKey.Should().Be("modified-api-key", "Settings should be reloaded when the file changes.");
        loadedConfig2.Should().NotBeSameAs(loadedConfig1, "A new settings instance should be created when the file changes.");
    }
    
    [TestMethod]
    public void LoadSettings_ShouldNotDecryptApiKey_WhenEnvironmentIsNotProduction()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development"); // Ensure this is set to non-production
        var appConfig = GetAppConfig();
        _configurationService.SaveSettings(appConfig);

        // Act
        var loadedConfig = _configurationService.LoadSettings(true);

        // Assert
        loadedConfig.GeneralSettings.OpenAiSettings.ApiKey.Should().Be("test-api-key", "The API key should remain as plain text in non-production environments.");
    }

    [TestMethod]
    public void LoadSettings_ShouldThrowException_WhenAppSettingsFileDoesNotExist()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockCryptoService = new Mock<ICryptoService>();
        

        // Simulate that user settings file does not exist
        mockFileSystem.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(false);
        
        var files = new Dictionary<string, string>
        {
            { $"appsettings.Production.json", "{}" }
        };

        var inMemoryFileProvider = new InMemoryFileProvider(files);
    
        // Create the configuration service with mocks
        var configurationService = new ConfigurationService(mockFileSystem.Object, mockCryptoService.Object, inMemoryFileProvider);

        // Act
        Action act = () => configurationService.LoadSettings(true);

        // Assert
        act.Should().Throw<FileNotFoundException>("because the settings file does not exist and the service should throw an exception.");
    }
    
    [TestMethod]
    public void SaveSettings_ShouldThrowException_WhenFileCannotBeWritten()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
    
        var appConfig = GetAppConfig();
    
        // Simulate that writing to the file throws UnauthorizedAccessException
        mockFileSystem.Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new UnauthorizedAccessException("File is read-only"));
    
        var configurationService = new ConfigurationService(mockFileSystem.Object);

        // Act
        Action act = () => configurationService.SaveSettings(appConfig);

        // Assert
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("File is read-only", "because the file is read-only and cannot be written to");
    }

    private static AppConfig GetAppConfig()
    {
        return new AppConfig
        {
            GeneralSettings = new GeneralSettings
            {
                OpenAiSettings = new OpenAiSettings
                {
                    ApiKey = "test-api-key"
                }
            }
        };
    }
}