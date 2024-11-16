namespace AiLogAnalyzer.Core.Services;

using AiLogAnalyzer.Core.Configuration;
using OpenAI.Chat;

public abstract class BaseLogAnalysisService(AppConfig appConfig) : ILogAnalysisService
{
    protected readonly AppConfig AppConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));

    protected List<ChatMessage> ChatHistory { get; } = [];

    public async Task<string> SendInitialMessage(string logData)
    {
        var basePrompt = string.IsNullOrEmpty(AppConfig.GeneralSettings.Prompt)
            ? Prompts.WebViewLogAnalysisPrompt
            : AppConfig.GeneralSettings.Prompt;

        var fullPrompt = $"{basePrompt}{logData}";

        return await SendMessageAndSaveHistory(fullPrompt);
    }

    public async Task<string> SendAdditionalMessage(string message)
    {
        var additionalMessage = $"Additional Message:\n{message}";
        return await SendMessageAndSaveHistory(additionalMessage);
    }

    private async Task<string> SendMessageAndSaveHistory(string message)
    {
        string result;
        Log.Info($"Sending message to AI:\n{message}");

        try
        {
            ChatHistory.Add(new UserChatMessage(message));
            result = await AnalyzeLog(message);
            ChatHistory.Add(new AssistantChatMessage(result));
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred while analyzing the log: {ex}");
            throw;
        }

        Log.Info($"AI response:\n {result}");
        return result;
    }


    public abstract Task<string> AnalyzeLog(string logData);
}