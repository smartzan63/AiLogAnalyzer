using System.Text;
using AiLogAnalyzer.Core.Configuration;
using OpenAI.Chat;

namespace AiLogAnalyzer.Core.Services;

public sealed class OpenAiLogAnalysisService : BaseLogAnalysisService
{
    private OpenAiSettings? _openAiSettings;
    private readonly IConfigurationService _configurationService;

    public OpenAiLogAnalysisService(IConfigurationService configurationService) : base(configurationService.LoadSettings())
    {
        _configurationService = configurationService;
        UpdateSettings();
    }
    
    private void UpdateSettings()
    {
        var appConfig = _configurationService.LoadSettings();
        _openAiSettings = appConfig.GeneralSettings.OpenAiSettings;

        if (string.IsNullOrEmpty(_openAiSettings.ApiKey))
        {
            Log.Warning(
                "OpenAI API key not found. Please set the API key in settings or as an environment variable 'OPENAI_API_KEY'.");
        }
    }

    public override async Task<string> AnalyzeLog(string logData)
    {
        var client = new ChatClient(_openAiSettings.CurrentModel, _openAiSettings.ApiKey);

        var chatCompletionsOptions = new ChatCompletionOptions
        {
            Temperature = AppConfig.GeneralSettings.Temperature,
            MaxOutputTokenCount = AppConfig.GeneralSettings.MaxTokens
        };

        var streamingUpdates = client.CompleteChatStreamingAsync(ChatHistory, chatCompletionsOptions);
        var fullResponse = new StringBuilder();

        await foreach (var update in streamingUpdates)
        {
            foreach (var part in update.ContentUpdate)
            {
                var partialResponse = part.Text ?? "";
                fullResponse.Append(partialResponse);
                OnPartialResponseReceived(partialResponse);
            }
        }

        var finalResponse = fullResponse.ToString();
        return finalResponse;
    }

    public event Action<string> PartialResponseReceived;

    private void OnPartialResponseReceived(string response)
    {
        PartialResponseReceived?.Invoke(response);
    }
}