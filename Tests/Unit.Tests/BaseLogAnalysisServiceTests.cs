namespace Unit.Tests;

using AiLogAnalyzer.Core;
using AiLogAnalyzer.Core.Configuration;
using AiLogAnalyzer.Core.Services;
using FluentAssertions;
using Moq;
using Serilog;
using OpenAI.Chat;

[TestClass]
public class BaseLogAnalysisServiceTests
{
    private readonly Mock<BaseLogAnalysisService> _mockService;

    private class TestLogAnalysisService(AppConfig appConfig) : BaseLogAnalysisService(appConfig)
    {
        public List<ChatMessage> GetChatHistory()
        {
            return base.ChatHistory;
        }

        public override Task<string> AnalyzeLog(string logData)
        {
            return Task.FromResult("Mock AI Response");
        }
    }

    public BaseLogAnalysisServiceTests()
    {
        var appConfig = new AppConfig();
        _mockService = new Mock<BaseLogAnalysisService>(appConfig) { CallBase = true };
    }

    [TestMethod]
    public async Task SendMessageAndSaveHistory_ShouldLogError_WhenExceptionIsThrown()
    {
        // Arrange
        const string logData = "test log data";
        const string exceptionMessage = "Test exception";
        var mockLogger = new Mock<ILogger>();

        AiLogAnalyzer.Core.Log.SetLogger(mockLogger.Object);
        _mockService.Setup(s => s.AnalyzeLog(It.IsAny<string>())).ThrowsAsync(new Exception(exceptionMessage));

        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<Exception>(
            () => _mockService.Object.SendInitialMessage(logData));

        Assert.AreEqual(exceptionMessage, exception.Message);
        mockLogger
            .Invocations
            .Should()
            .Contain(i => i.Arguments[0].ToString()!.Contains(exceptionMessage));
    }

    [TestMethod]
    public void Constructor_ShouldThrowArgumentNullException_WhenAppConfigIsNull()
    {
        // Arrange
        AppConfig nullAppConfig = null!;

        // Act & Assert
        var exception = Assert.ThrowsException<ArgumentNullException>(() =>
        {
            _ = new TestLogAnalysisService(nullAppConfig);
        });

        Assert.AreEqual("appConfig", exception.ParamName);
    }
    
    [TestMethod]
    public async Task SendInitialMessage_ShouldUseWebViewLogAnalysisPrompt_WhenAppConfigPromptIsEmpty()
    {
        // Arrange
        var appConfig = new AppConfig
        {
            GeneralSettings = new GeneralSettings { Prompt = string.Empty }
        };

        var service = new TestLogAnalysisService(appConfig);
        
        // Act
        _ = await service.SendInitialMessage("log data");

        // Assert
        var chatHistory = service.GetChatHistory();
        chatHistory.Should().HaveCount(2); // Should contain user and assistant messages

        // Check that the user message was added correctly
        chatHistory
            .First()
            .Should()
            .BeOfType<UserChatMessage>()
            .Which
            .Content[0]
            .Text
            .Should()
            .Contain(Prompts.WebViewLogAnalysisPrompt);

        // Check that the assistant message was added correctly
        chatHistory
            .Last()
            .Should()
            .BeOfType<AssistantChatMessage>()
            .Which.Content[0]
            .Text
            .Should()
            .Be("Mock AI Response");
    }

    [TestMethod]
    public async Task SendInitialMessage_ShouldUseAppConfigPrompt_WhenAppConfigPromptIsSet()
    {
        // Arrange
        const string customPrompt = "Custom prompt";
        var appConfig = new AppConfig
        {
            GeneralSettings = new GeneralSettings { Prompt = customPrompt }
        };

        var service = new TestLogAnalysisService(appConfig);
        
        // Act
        _ = await service.SendInitialMessage("log data");

        // Assert
        var chatHistory = service.GetChatHistory();

        chatHistory
            .First()
            .Should()
            .BeOfType<UserChatMessage>()
            .Which
            .Content[0]
            .Text
            .Should()
            .Contain("Custom prompt");
    }
}