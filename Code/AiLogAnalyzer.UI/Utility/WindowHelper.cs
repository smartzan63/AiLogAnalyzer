namespace AiLogAnalyzer.UI.Utility;

using System;
using System.Threading.Tasks;
using Windows.Graphics;
using H.NotifyIcon;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Core;
using Components;

public static class WindowHelper
{
    public static void AdjustWindowSizeToScale(Window window, int baseWidth, int baseHeight)
    {
        var appWindow = window.AppWindow;

        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        var rasterizationScale = displayArea.WorkArea.Width / System.Windows.SystemParameters.PrimaryScreenWidth;

        Log.Debug($"Showing window {window.Content.GetType().Name} with rasterization scale: {rasterizationScale}");

        var scaledWidth = (int)(baseWidth * rasterizationScale);
        var scaledHeight = (int)(baseHeight * rasterizationScale);

        appWindow.Resize(new SizeInt32(scaledWidth, scaledHeight));
    }

    public static async Task ShowMessageDialogAsync(XamlRoot xamlRoot, string content, string title)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "OK",
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();
    }

    public static void CenterWindowOnScreen(AppWindow appWindow)
    {
        var originalSize = appWindow.Size;

        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        var centerX = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
        var centerY = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;

        appWindow.Move(new PointInt32(centerX, centerY));

        appWindow.Resize(originalSize);
    }

    public static void CustomizeTitleBar(AppWindow appWindow)
    {
        appWindow.SetIcon("Resources/WB_ICON.ico");
        appWindow.Title = "Log Analyzer";

        var titleBar = appWindow.TitleBar;
        titleBar.ExtendsContentIntoTitleBar = false;

        titleBar.BackgroundColor = Colors.Black;
        titleBar.ForegroundColor = Colors.DarkGray;

        titleBar.InactiveBackgroundColor = Colors.Black;
        titleBar.InactiveForegroundColor = Colors.DarkGray;

        titleBar.ButtonBackgroundColor = Colors.Black;
        titleBar.ButtonForegroundColor = Colors.DarkGray;
        titleBar.ButtonHoverBackgroundColor = Colors.Black;
        titleBar.ButtonHoverForegroundColor = Colors.White;
        titleBar.ButtonPressedBackgroundColor = Colors.Black;
        titleBar.ButtonPressedForegroundColor = Colors.DarkGray;
        titleBar.ButtonInactiveBackgroundColor = Colors.Black;
        titleBar.ButtonInactiveForegroundColor = Colors.DarkGray;
    }

    public static async Task<ContentDialogResult> ShowYesNoDialogAsync(XamlRoot xamlRoot, string content, string title)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            XamlRoot = xamlRoot
        };

        return await dialog.ShowAsync();
    }

    public static async Task<ContentDialogResult> ShowNotificationAsync(string title, string content,
        string closeButtonText, XamlRoot xamlRoot = null)
    {
        Window tempWindow = null;

        if (xamlRoot == null)
        {
            tempWindow = new Window
            {
                Content = new DummyPage()
            };

            var appWindow = tempWindow.AppWindow;

            tempWindow.Hide();
            tempWindow.Activate();
            appWindow.Show();

            xamlRoot = tempWindow.Content.XamlRoot;
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = closeButtonText,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();

        tempWindow?.Close();

        return result;
    }
}