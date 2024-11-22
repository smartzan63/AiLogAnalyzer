namespace AiLogAnalyzer.Core.Services;

using Configuration;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

public sealed class OllamaLogAnalysisService : BaseLogAnalysisService
{
    private readonly IConfigurationService _configurationService;
    private OllamaSettings _ollamaSettings;

    public OllamaLogAnalysisService(IConfigurationService configurationService) : base(configurationService.LoadSettings())
    {
        _configurationService = configurationService;
        UpdateSettings();
    }

    private void UpdateSettings()
    {
        var appConfig = _configurationService.LoadSettings();
        _ollamaSettings = appConfig.GeneralSettings.OllamaSettings;

        if (string.IsNullOrEmpty(_ollamaSettings.Host))
        {
            Log.Warning("Ollama host not found. Please set the host in settings or as an environment variable 'OLLAMA_HOST'.");
        }
        else
        {
            Log.Info($"Ollama host: {_ollamaSettings.Host}");
        }
    }

    public override async Task<string> AnalyzeLog(string logData)
    {
        return await Task.Run<string>(async () =>
        {
            var uri = _ollamaSettings.Host;
            var model = _ollamaSettings.CurrentModel;
            var ollama = new OllamaApiClient(uri);

            var options = new RequestOptions
            {
                Temperature = AppConfig.GeneralSettings.Temperature
            };

            var chatRequest = new ChatRequest
            {
                Model = model,
                Messages = new List<Message> { new(role: "user", content: logData) },
                Options = options
            };

            string response = string.Empty;

            await foreach (var chatResponse in ollama.StreamChat(chatRequest, CancellationToken.None))
            {
                if (chatResponse != null)
                {
                    var partialResponse = chatResponse.Message.Content;
                    response += chatResponse.Message.Content;

                    OnPartialResponseReceived(partialResponse);
                }
            }

            return response;
        });
    }

    public event Action<string> PartialResponseReceived;

    private void OnPartialResponseReceived(string response)
    {
        PartialResponseReceived?.Invoke(response);
    }
}