namespace AiLogAnalyzer.Core.Services;

public class FileCollectorService : IFileCollectorService
{
    public async Task<List<string>> CollectFilesAsync(string directoryPath, List<string> fileNames)
    {
        var collectedFiles = new List<string>();

        try
        {
            // Get all files in the specified directory
            var allFiles = Directory.GetFiles(directoryPath);

            foreach (var file in allFiles)
            {
                // Check if the file name matches any of the names in the list
                if (fileNames.Any(fileName => Path.GetFileName(file).Contains(fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    collectedFiles.Add(file);
                }
            }
        }
        
        catch (Exception ex)
        {
            Log.Error($"Error collecting files: {ex.Message}");
        }

        return collectedFiles;
    }
}