namespace AiLogAnalyzer.Core.Configuration;

public class OllamaSettings
{
    public string Host { get; set; }
    
    public static List<string> ModelList { get; set; }
    
    public string CurrentModel { get; set; }
}