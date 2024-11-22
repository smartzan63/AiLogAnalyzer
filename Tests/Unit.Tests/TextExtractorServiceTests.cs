namespace Unit.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AiLogAnalyzer.Core.Services;
using FluentAssertions;

[TestClass]
public class TextExtractorServiceTests
{
    private TextExtractorService _textExtractorService;
    private string TestDataFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "DataCollector");

    [TestInitialize]
    public void Initialize()
    {
        _textExtractorService = new TextExtractorService();
    }

    [TestMethod]
    public async Task ExtractTextAsync_WhenDirectoryPathIsValidAndFileNamesMatch_ReturnsCollectedText()
    {
        // Arrange
        var fileNames = new List<string> { "file1.txt", "file2.txt" };

        // Act
        var extractedText = await _textExtractorService.ExtractTextAsync(TestDataFolder, fileNames);

        // Assert
        extractedText.Should().Contain("#FileName: file1.txt");
        extractedText.Should().Contain("Content of file1.txt");
        extractedText.Should().Contain("#FileName: file2.txt");
        extractedText.Should().Contain("Content of file2.txt");
    }
}
