namespace Unit.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AiLogAnalyzer.Core.Services;
using FluentAssertions;

[TestClass]
public class FileCollectorServiceTests
{
    private FileCollectorService _fileCollectorService;
    private string TestDataFolder = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "DataCollector");

    [TestInitialize]
    public void Initialize()
    {
        _fileCollectorService = new FileCollectorService();
    }

    [TestMethod]
    public async Task CollectFilesAsync_WhenDirectoryPathIsValidAndFileNamesMatch_ReturnsCollectedFiles()
    {
        // Arrange
        var fileNames = new List<string> { "file1.txt", "file2.txt" };

        // Act
        var collectedFiles = await _fileCollectorService.CollectFilesAsync(TestDataFolder, fileNames);

        // Assert
        collectedFiles.Count.Should().Be(2);

        var expectedFiles = new List<string>
        {
            Path.Combine(TestDataFolder, "file1.txt"),
            Path.Combine(TestDataFolder, "file2.txt")
        };
        CollectionAssert.AreEqual(expectedFiles, collectedFiles);
    }
}