namespace AiLogAnalyzer.Core.Services;

public interface ITextExtractorService
{
    Task<string> ExtractTextAsync(string directoryPath, List<string> fileNames);
}
