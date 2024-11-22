namespace AiLogAnalyzer.Core.Services;

using System.Text;

public class TextExtractorService : ITextExtractorService
{
    public async Task<string> ExtractTextAsync(string directoryPath, List<string> fileNames)
    {
        var collectedText = new StringBuilder();

        try
        {
            // Get all files in the specified directory
            var allFiles = Directory.GetFiles(directoryPath);

            foreach (var file in allFiles)
            {
                // Check if the file name matches any of the names in the list
                if (fileNames.Any(fileName => Path.GetFileName(file).Contains(fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    collectedText.AppendLine($"#FileName: {Path.GetFileName(file)}");
                    
                    // Read and append the content of the file
                    var fileContent = await File.ReadAllTextAsync(file);
                    collectedText.AppendLine(fileContent);
                    collectedText.AppendLine(); // Add a blank line for separation
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error extracting text from files: {ex.Message}");
        }

        return collectedText.ToString();
    }
}
