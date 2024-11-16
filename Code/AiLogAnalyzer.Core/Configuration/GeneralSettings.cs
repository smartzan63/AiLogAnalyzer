namespace AiLogAnalyzer.Core.Configuration;

public class GeneralSettings
{
    public AiSource AiSource { get; set; } = AiSource.OpenAiApi;
    public string Prompt { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 2000;
    public float Temperature { get; set; } = 0.0f;
    
    public OpenAiSettings OpenAiSettings { get; set; } = new ();
    
    public OllamaSettings OllamaSettings { get; set; } = new ();
}