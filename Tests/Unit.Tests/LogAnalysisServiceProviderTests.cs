namespace Unit.Tests;

using AiLogAnalyzer.Core.Configuration;
using AiLogAnalyzer.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

[TestClass]
public class LogAnalysisServiceProviderTests
{
    private AppConfig _config;
    private LogAnalysisServiceProvider _provider;
    private readonly IServiceProvider _serviceProvider;

    public LogAnalysisServiceProviderTests()
    {
        _config = new AppConfig { GeneralSettings = new GeneralSettings { AiSource = AiSource.Ollama } };

        var services = new ServiceCollection();

        services.AddTransient<OpenAiLogAnalysisService>();
        services.AddTransient<OllamaLogAnalysisService>();
        services.AddSingleton<IRegistryService, RegistryService>();
        services.AddSingleton<ICryptoService, CryptoService>();
        
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IConfigurationBuilder, ConfigurationBuilder>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IFileProvider>(new PhysicalFileProvider(AppContext.BaseDirectory));

        services.AddSingleton<LogAnalysisServiceProvider>();

        _serviceProvider = services.BuildServiceProvider();
        _serviceProvider.GetRequiredService<IConfigurationService>().SaveSettings(_config);

        _provider = new LogAnalysisServiceProvider(_serviceProvider);
    }

    [TestMethod]
    public void GetService_ReturnsCorrectService_BasedOnDefaultConfig()
    {
        // Act
        var service = _provider.GetService();

        // Assert
        Assert.IsInstanceOfType(service, typeof(OllamaLogAnalysisService));
    }

    [TestMethod]
    public void UpdateService_SwitchesBackAndForth()
    {
        // Arrange
        var configurationService = _serviceProvider.GetRequiredService<IConfigurationService>();

        // Act & Assert
        var aiSources = new[] { AiSource.OpenAiApi, AiSource.Ollama };
        var expectedTypes = new[] { typeof(OpenAiLogAnalysisService), typeof(OllamaLogAnalysisService) };

        for (var i = 0; i < aiSources.Length; i++)
        {
            _config.GeneralSettings.AiSource = aiSources[i];
            configurationService.SaveSettings(_config);
            _provider.UpdateConfig();
            var service = _provider.GetService();
            Assert.IsInstanceOfType(service, expectedTypes[i]);
        }
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void CreateService_ThrowsException_ForUnsupportedAiSource()
    {
        // Arrange
        _config = new AppConfig { GeneralSettings = new GeneralSettings { AiSource = (AiSource)999 } };

        _serviceProvider.GetService<IConfigurationService>()?.SaveSettings(_config);

        _provider = new LogAnalysisServiceProvider(_serviceProvider);

        // Act
        _provider.GetService();
    }
    
    [TestMethod]
    public void CreateService_LoadsAiSourceFromConfig_WhenAiSourceIsNull()
    {
        // Arrange
        _config = new AppConfig { GeneralSettings = new GeneralSettings { AiSource = AiSource.OpenAiApi } };

        var configurationService = _serviceProvider.GetRequiredService<IConfigurationService>();
        configurationService.SaveSettings(_config);

        _provider = new LogAnalysisServiceProvider(_serviceProvider);

        // Act
        var service = _provider.GetType()
            .GetMethod("CreateService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(_provider, new object[] { null });

        // Assert
        Assert.IsNotNull(service);
        Assert.IsInstanceOfType(service, typeof(OpenAiLogAnalysisService));
    }

}