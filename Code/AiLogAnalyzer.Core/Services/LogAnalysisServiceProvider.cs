namespace AiLogAnalyzer.Core.Services;

using AiLogAnalyzer.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class LogAnalysisServiceProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationService _configurationService;
    private ILogAnalysisService _currentService;

    public LogAnalysisServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _configurationService = _serviceProvider.GetRequiredService<IConfigurationService>();
        var appConfig = _configurationService.LoadSettings();
        _currentService = CreateService(appConfig.GeneralSettings.AiSource);
    }

    public void UpdateConfig()
    {
        var newConfig = _configurationService.LoadSettings(true);
        _currentService = CreateService(newConfig.GeneralSettings.AiSource);
    }

    public ILogAnalysisService GetService()
    {
        return _currentService;
    }
    
    private ILogAnalysisService CreateService(AiSource? aiSource = null)
    {
        if (!aiSource.HasValue)
        {
            var config = _configurationService.LoadSettings(true);
            aiSource = config.GeneralSettings.AiSource;
        }
        
        return aiSource switch
        {
            AiSource.Ollama => _serviceProvider.GetRequiredService<OllamaLogAnalysisService>(),
            AiSource.OpenAiApi => _serviceProvider.GetRequiredService<OpenAiLogAnalysisService>(),
            _ => throw new InvalidOperationException("Unsupported AI source.")
        };
    }
}