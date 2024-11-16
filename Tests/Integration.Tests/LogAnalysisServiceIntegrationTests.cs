using Moq;

namespace Integration.Tests;

using AiLogAnalyzer.Core.Services;
using Common.Tests;

[TestClass]
public class LogAnalysisServiceIntegrationTests
{
    private OpenAiLogAnalysisService _openAiLogAnalysisService;
    private OllamaLogAnalysisService _ollamaLogAnalysisService;
    private Mock<IConfigurationService> _configurationServiceMock;

    private const string LogData = "Null reference exception thrown in method Foo()";

    [TestInitialize]
    public void Setup()
    {
        _configurationServiceMock = new Mock<IConfigurationService>();

        var appConfig = TestAssemblyInitializer.AppConfig;

        _configurationServiceMock.Setup(service => service.LoadSettings(It.IsAny<bool>())).Returns(appConfig);

        _openAiLogAnalysisService = new OpenAiLogAnalysisService(_configurationServiceMock.Object);
        _ollamaLogAnalysisService = new OllamaLogAnalysisService(_configurationServiceMock.Object);
    }

    [TestMethod]
    public async Task SendInitialMessage_ReturnsResponseFromOpenAi()
    {
        // Act
        var result = await _openAiLogAnalysisService.SendInitialMessage(LogData);

        // Assert
        LogAnalysisServiceResponseValidator.ValidateBasicResponse(result);
    }

    [TestMethod]
    public async Task SendAdditionalMessage_ReturnsResponseFromOpenAi()
    {
        // Act
        var result = await _openAiLogAnalysisService.SendAdditionalMessage(LogData);

        // Assert
        LogAnalysisServiceResponseValidator.ValidateAdditionalResponse(result);
    }

    [Ignore]
    [TestMethod]
    public async Task SendInitialMessage_ReturnsResponseFromOllama()
    {
        // Act
        var result = await _ollamaLogAnalysisService.SendInitialMessage(LogData);

        // Assert
        LogAnalysisServiceResponseValidator.ValidateBasicResponse(result);
    }

    [Ignore]
    [TestMethod]
    public async Task SendAdditionalMessage_ReturnsResponseFromOllama()
    {
        // Act
        var result = await _ollamaLogAnalysisService.SendAdditionalMessage(LogData);

        // Assert
        LogAnalysisServiceResponseValidator.ValidateAdditionalResponse(result);
    }
}