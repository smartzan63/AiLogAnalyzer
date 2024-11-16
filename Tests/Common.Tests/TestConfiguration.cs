namespace Common.Tests;

using AiLogAnalyzer.Core.Configuration;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class TestConfiguration
{
    public static AppConfig GetAppConfig()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<TestConfiguration>(optional: true)
            .Build();

        var openAiSettings = new OpenAiSettings
        {
            CurrentModel = config["OpenAiSettings:CurrentModel"],
            ApiKey = config["OpenAiSettings:ApiKey"]
        };
        
        var ollamaSettings = new OllamaSettings
        {
            Host = config["OllamaSettings:Host"],
            CurrentModel = config["OllamaSettings:CurrentModel"]
        };

        var appConfig = new AppConfig
        {
            GeneralSettings = new GeneralSettings
            {
                OpenAiSettings = openAiSettings,
                OllamaSettings = ollamaSettings,
                Temperature = 0.5f,
                MaxTokens = 100
            }
        };

        return appConfig;
    }
}