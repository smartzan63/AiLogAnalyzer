using AiLogAnalyzer.Core;

namespace AiLogAnalyzer.UI.Managers;

using System;
using System.IO;
using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

public class TrayIconManager : IDisposable
{
    private readonly TaskbarIcon _notifyIcon;

    public TrayIconManager(Action onExitClick, Action onSettingsClick)
    {
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "WB_ICON.ico");
            var iconUri = new Uri(iconPath);

            _notifyIcon = new TaskbarIcon
            {
                IconSource = new BitmapImage(iconUri),
                ToolTipText = "Log Analyzer"
            };

            var contextMenu = CreateContextMenu(onExitClick, onSettingsClick);
            _notifyIcon.ContextFlyout = contextMenu;
            _notifyIcon.ContextMenuMode = ContextMenuMode.SecondWindow;
            _notifyIcon.ForceCreate(false);
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred while creating the tray icon: {ex.Message}");
        }
    }

    private MenuFlyout CreateContextMenu(Action onExitClick, Action onSettingsClick)
    {
        try
        {
            var contextMenu = new MenuFlyout();

            var settingsMenuItem = new MenuFlyoutItem { Text = "Settings" };
            settingsMenuItem.Click += (sender, args) => onSettingsClick();
            contextMenu.Items.Add(settingsMenuItem);

            var exitMenuItem = new MenuFlyoutItem { Text = "Exit" };
            exitMenuItem.Click += (sender, args) => onExitClick();
            contextMenu.Items.Add(exitMenuItem);

            return contextMenu;
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred while creating the context menu: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _notifyIcon.Dispose();
    }
}