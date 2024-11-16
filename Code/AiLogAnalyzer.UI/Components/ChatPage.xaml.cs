namespace AiLogAnalyzer.UI.Components;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Core;
using Core.Services;
using Utility;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

//TODO:
// Feature: Send button should work with Enter key
// Feature: Add labels to the messages, like user message, agent message, etc.

public sealed partial class ChatPage : Page
{
    private readonly ILogAnalysisService _logAnalysisService;
    private readonly SemaphoreSlim _webViewSemaphore = new(1, 1);
    private StringBuilder _htmlBuffer = new();
    private bool _isStartedProcessing;

    public ChatPage(ILogAnalysisService logAnalysisService, HotKeyHandler hotKeyHandler)
    {
        InitializeComponent();
        _logAnalysisService = logAnalysisService;
        Loaded += ChatPage_Loaded;

        switch (_logAnalysisService)
        {
            case OllamaLogAnalysisService ollamaService:
                ollamaService.PartialResponseReceived += OnPartialResponseReceived;
                Log.Info("Ollama service is used.");
                break;
            case OpenAiLogAnalysisService openAiService:
                openAiService.PartialResponseReceived += OnPartialResponseReceived;
                Log.Info("OpenAI service is used.");
                break;
        }
    }

    private async void ChatPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isStartedProcessing)
        {
            return;
        }

        _isStartedProcessing = true;
        var logData = await Windows.ApplicationModel.DataTransfer.Clipboard.GetContent().GetTextAsync();
        Log.Info($"Clipboard data: {logData}");
        var initialContent = JsWrappedScriptsLoader.GetResponsiveHtmlContent($"<div class=userMessage>{logData}</div>");
        
        await MessagesWebView.EnsureCoreWebView2Async();
        
        //MessagesWebView.CoreWebView2.OpenDevToolsWindow();
        MessagesWebView.NavigateToString(initialContent);

        SendButton.IsEnabled = false;
        await AnalyzeAndShowLog(logData);
        _isStartedProcessing = false;
        SendButton.IsEnabled = true;
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        var message = InputTextBox.Text;
        InputTextBox.Text = string.Empty;

        SendButton.IsEnabled = false;
        await SendAdditionalMessageAndShow(message);
        SendButton.IsEnabled = true;
    }

    private async Task AnalyzeAndShowLog(string logData)
    {
        try
        {
            var initialMessage = $"Initial Message:\n{logData}";
            var result = await _logAnalysisService.SendInitialMessage(initialMessage);
            await UpdateMessagesOnWebView(result, isUserMessage: false);
        }
        catch (Exception ex)
        {
            await UpdateMessagesOnWebView("Error:\n" + ex.Message, isUserMessage: false);
        }
    }

    private async Task SendAdditionalMessageAndShow(string message)
    {
        try
        {
            Log.Debug($"Sending additional message: {message}");
            _htmlBuffer = new StringBuilder();
            
            var protectedMessage = ProtectString(message);
            await UpdateMessagesOnWebView(protectedMessage, isUserMessage: true);
            
            var result = await _logAnalysisService.SendAdditionalMessage(message);
            await UpdateMessagesOnWebView(result, isUserMessage: false);
        }
        catch (Exception ex)
        {
            await UpdateMessagesOnWebView("Error:\n" + ex.Message, isUserMessage: false);
        }
    }

    private void OnPartialResponseReceived(string response)
    {
        try
        {
            DispatcherQueue.TryEnqueue(async () => { await AppendToAiResponse(response); });
        }
        catch (Exception ex)
        {
            Log.Error($"Error while processing partial response, exception: {ex}");
        }
    }

    // When Enter + Ctrl key is pressed, it should work as Send button
    // When Enter key is pressed, it should insert a new line
    // Currently it sends the message when Enter key is pressed
    // But it inserts a new line only once, after that if enter pressed again, it returns cursor to the end of the previous line, but if you type something, it will be inserted to the new line
    private void textBox_onKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            var controlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) &
                               CoreVirtualKeyStates.Down;

            if (controlState == CoreVirtualKeyStates.Down)
            {
                e.Handled = true;
                Send_Click(sender, e);
            }
            else
            {
                e.Handled = true;

                var textBox = sender as TextBox;
                if (textBox == null) return;
                
                // Get the current selection start position
                int cursorPosition = textBox.SelectionStart;
                
                // Insert a new line at the cursor position
                textBox.Text = textBox.Text.Insert(cursorPosition, "\n");
                
                // Update the selection start and end position
                textBox.SelectionStart = cursorPosition + 2;
                textBox.SelectionLength = 0;
            }
        }
    }

    private async Task UpdateMessagesOnWebView(string message, bool isUserMessage = false)
    {
        await _webViewSemaphore.WaitAsync();
        try
        {
            if (isUserMessage)
            {
                var script = JsWrappedScriptsLoader.ShowUserMessageScript(message);
                await MessagesWebView.ExecuteScriptAsync(script);
            }
            else
            {
                var script = JsWrappedScriptsLoader.ShowAgentMessageScript(message);
                await MessagesWebView.ExecuteScriptAsync(script);
            }
        }
        finally
        {
            _webViewSemaphore.Release();
        }
    }

    private async Task AppendToAiResponse(string message)
    {
        var accumulatedMessage = _htmlBuffer.Append(message).ToString();
        accumulatedMessage = ProtectString(accumulatedMessage);

        await UpdateMessagesOnWebView(accumulatedMessage, isUserMessage: false);
    }

    private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var newWidth = e.NewSize.Width;
        if (MessagesWebView == null) return;
        
        MessagesWebView.Width = newWidth;
        MessagesWebView.MinWidth = newWidth;
        MessagesWebView.MaxWidth = newWidth;
    }

    private static string ProtectString(string message)
    {
        return message
            .Replace("\\", @"\\")
            .Replace("`", "\\`")
            .Replace("\r", @"\n\r")
            .Replace("\n", "\\n")  
            //.Replace("<", "&lt;")
            //.Replace(">", "&gt;")
            .Replace("$", "\\$")
            .Replace("=", "\\=")
            .Replace("'", "\\'")
            .Replace("\"", "\\\"");
    }
}