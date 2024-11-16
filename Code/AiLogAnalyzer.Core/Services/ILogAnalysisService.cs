namespace AiLogAnalyzer.Core.Services;

public interface ILogAnalysisService
{
    Task<string> SendInitialMessage(string message);
    Task<string> SendAdditionalMessage(string message);
}