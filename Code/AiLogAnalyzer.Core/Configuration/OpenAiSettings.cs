namespace AiLogAnalyzer.Core.Configuration;

public class OpenAiSettings
{
    public static List<string> ModelList { get; set; }
    
    public string? CurrentModel { get; set; }
    
    public string? ApiKey { get; set; }
}