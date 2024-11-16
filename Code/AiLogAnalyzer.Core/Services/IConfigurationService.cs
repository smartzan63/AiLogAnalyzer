using AiLogAnalyzer.Core.Configuration;

namespace AiLogAnalyzer.Core.Services;

public interface IConfigurationService
{
    AppConfig LoadSettings(bool forceReload = false);
    void SaveSettings(AppConfig settings);
}
