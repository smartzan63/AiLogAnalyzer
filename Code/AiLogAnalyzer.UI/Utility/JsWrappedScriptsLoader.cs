namespace AiLogAnalyzer.UI.Utility;

using System;
using System.IO;
using Core;

public static class JsWrappedScriptsLoader
{
    private const string ResourcesFolder = "JavaScript";

    // TODO: Move this method to a common utility class

    public static string ShowAgentMessageScript(string accumulatedMessage)
    {
        return ShowMessageWithHighlightsScript("showAgentMessageWithHighlights", accumulatedMessage);
    }

    public static string ShowUserMessageScript(string message)
    {
        return ShowMessageWithHighlightsScript("showUserMessage", message);
    }

    public static string GetResponsiveHtmlContent(string markdown)
    {
        var htmlTemplate = GetEmbeddedResource("ChatWindow.html");
        if (htmlTemplate == null)
        {
            const string message = "ChatWindow.html not found as embedded resource.";
            Log.Error(message);
            throw new ApplicationException(message);
        }

        var scriptContent = GetEmbeddedResource("scripts.js");
        if (scriptContent == null)
        {
            const string message = "scripts.js not found as embedded resource.";
            Log.Error(message);
            throw new ApplicationException(message);
        }

        htmlTemplate = htmlTemplate.Replace("</head>", $"<script>{scriptContent}</script></head>");

        return htmlTemplate.Replace("{markdown}", markdown);
    }

    private static string ShowMessageWithHighlightsScript(string functionName, string message)
    {
        var updateScript = @$"
          try {{
              {functionName}('{message}');
          }} catch(e) {{
              console.error('Error while loading scripts:', e);
          }}
          ";
        return updateScript;
    }

    private static string GetEmbeddedResource(string resourceName)
    {
        var resource = Path.Combine(ResourcesFolder, resourceName);

        return File.ReadAllText(resource);
    }
}