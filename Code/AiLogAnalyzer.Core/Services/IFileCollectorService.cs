namespace AiLogAnalyzer.Core.Services;

public interface IFileCollectorService
{
    Task<List<string>> CollectFilesAsync(string directoryPath, List<string> fileNames);
}