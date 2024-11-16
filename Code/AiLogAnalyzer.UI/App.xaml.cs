﻿using System.IO.Abstractions;
using AiLogAnalyzer.UI.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AiLogAnalyzer.UI;

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using AiLogAnalyzer.Core.Services;
using AiLogAnalyzer.UI.Managers;
using AiLogAnalyzer.Core;
using AiLogAnalyzer.Core.Configuration;
using AiLogAnalyzer.UI.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public partial class App : Application
{
    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();

    private static readonly Mutex Mutex = new(true, "{A0C2D0FE-CE9F-4E74-BBDF-3EFC8B3B28D3}");
    public static App Instance { get; private set; }

    public readonly IHost Host;

    public HotKeyHandler HotKeyHandler { get; set; } = null!;

    private static Window WelcomeWindow { get; set; }
    public static Window SettingsWindow { get; private set; }

    public static Window ChatWindow { get; private set; }

    private TrayIconManager _trayIconManager;

    public App()
    {
        SetProcessDPIAware();

        Log.Info("Starting application...");
        try
        {
            this.InitializeComponent();
            Instance = this;
            Host = CreateHostBuilder().Build();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }

    public void OnHotKeyPressed()
    {
        Log.Info("Hot key pressed");

        if (ChatWindow == null || ChatWindow.AppWindow == null)
        {
            var logAnalysisServiceProvider = Host.Services.GetRequiredService<LogAnalysisServiceProvider>();
            var logAnalysisService = logAnalysisServiceProvider.GetService();

            ChatWindow = new Window
            {
                Title = "Log Analyzer",
                Content = new ChatPage(logAnalysisService, HotKeyHandler)
            };

            var appWindow = ChatWindow.AppWindow;
            appWindow.Resize(new SizeInt32(1200, 900));

            WindowHelper.CustomizeTitleBar(appWindow);
            WindowHelper.CenterWindowOnScreen(appWindow);

            ChatWindow.Activate();
        }
        else
        {
            Log.Info("Main window is already open, focusing on it");
            ChatWindow.Activate();
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        ShowWelcomeMessageAndLoadConfiguration();
    }

    private async void ShowWelcomeMessageAndLoadConfiguration()
    {
        ShowWelcomeMessage();

        if (!Mutex.WaitOne(TimeSpan.Zero, true))
        {
            Log.Info("Application is already running, showing notification...");

            while (WelcomeWindow.Content.XamlRoot == null)
            {
                await Task.Delay(100);
            }

            await WindowHelper.ShowNotificationAsync(
                title: "Application Already Running",
                content: "The application is already running. Please use the tray icon to access it.",
                closeButtonText: "OK",
                xamlRoot: WelcomeWindow.Content.XamlRoot
            );
            Application.Current.Exit();

            return;
        }

        var loadConfigurationTask = LoadConfigurationAndSetupAsync();
        var showWelcomeMessageTask = Task.Delay(TimeSpan.FromSeconds(1));

        await Task.WhenAll(loadConfigurationTask, showWelcomeMessageTask);

        var settingsManager = Host.Services.GetRequiredService<SettingsManager>();
        var appConfig = settingsManager.LoadSettings();

        try
        {
            if (string.IsNullOrEmpty(appConfig.GeneralSettings.OpenAiSettings.ApiKey))
            {
                Log.Warning("OpenAI API key is missing.");
                var result = await WindowHelper.ShowYesNoDialogAsync(
                    WelcomeWindow.Content.XamlRoot,
                    content: "OpenAI API key is not set. Do you want to open settings and set the key?",
                    title: "Missing API Key");

                if (result == ContentDialogResult.Primary)
                {
                    OpenSettingsWindow();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
        finally
        {
            WelcomeWindow.Close();
            Log.Info("Application is ready");
        }
    }

    private async Task LoadConfigurationAndSetupAsync()
    {
        Log.Info("Loading configuration and setting up the app");
        try
        {
            await Host.StartAsync();

            var settingsManager = Host.Services.GetRequiredService<SettingsManager>();
            var appConfig = settingsManager.LoadSettings();

            _trayIconManager = new TrayIconManager(OnExitClick, OnSettingsClick);

            HotKeyHandler = new HotKeyHandler(appConfig);
            HotKeyHandler.HotKeyPressed += OnHotKeyPressed;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }

    private static void ShowWelcomeMessage()
    {
        Log.Info("Showing welcome message");
        try
        {
            WelcomeWindow = new Window
            {
                Content = new WelcomeMessagePage()
            };

            var appWindow = WelcomeWindow.AppWindow;
            appWindow.Resize(new SizeInt32(480, 590));

            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = false;
                presenter.IsMinimizable = false;
                presenter.IsMaximizable = false;
                presenter.SetBorderAndTitleBar(false, false);
            }

            WindowHelper.CenterWindowOnScreen(appWindow);
            appWindow.Show();
            WelcomeWindow.Activate();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }

    private void OnExitClick()
    {
        _trayIconManager.Dispose();
        Application.Current.Exit();
    }

    private void OnSettingsClick()
    {
        OpenSettingsWindow();
    }

    private void OpenSettingsWindow()
    {
        try
        {
            var settingsManager = Host.Services.GetRequiredService<SettingsManager>();
            var appConfig = settingsManager.LoadSettings();

            SettingsWindow = new Window
            {
                Title = "Settings",
                Content = new SettingsPage(appConfig)
            };

            var appWindow = SettingsWindow.AppWindow;
            appWindow.Resize(new SizeInt32(600, 750));

            WindowHelper.CustomizeTitleBar(appWindow);
            WindowHelper.CenterWindowOnScreen(appWindow);

            SettingsWindow.Activate();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }

    private static IHostBuilder CreateHostBuilder()
        => Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IConfigurationService, ConfigurationService>();
                services.AddSingleton<IConfigurationBuilder, ConfigurationBuilder>();
                services.AddSingleton<IRegistryService, RegistryService>();
                services.AddSingleton<ICryptoService, CryptoService>();
                services.AddSingleton<IFileSystem, FileSystem>();
                services.AddSingleton<SettingsManager>();
                
                services.AddSingleton<LogAnalysisServiceProvider>();
                services.AddTransient<OllamaLogAnalysisService>();
                services.AddTransient<OpenAiLogAnalysisService>();
            });
}