namespace AiLogAnalyzer.UI.Components;

using System;
using System.Linq;
using Windows.System;
using Core;
using Core.Configuration;
using Managers;
using Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public sealed partial class SettingsPage : Page
{
    private readonly AppConfig _appConfig;

    public SettingsPage(AppConfig appConfig)
    {
        InitializeComponent();
        _appConfig = appConfig;
        InitializeSettingsControls();
    }

    private void InitializeSettingsControls()
    {
        try
        {
            var listOfVirtualKeys = Enum.GetValues(typeof(VirtualKey)).Cast<VirtualKey>().Select(vk => vk.ToString())
                .ToList();
            var aiSources = Enum.GetValues(typeof(AiSource)).Cast<AiSource>().Select(source => source.ToString())
                .ToList();


            cmbMainKey.ItemsSource = listOfVirtualKeys;
            cmbModifierKey1.ItemsSource = listOfVirtualKeys;
            cmbModifierKey2.ItemsSource = listOfVirtualKeys;
            cmbAiSource.ItemsSource = aiSources;

            cmbMainKey.SelectedItem = _appConfig.HotKeySettings.MainKey;
            cmbModifierKey1.SelectedItem = _appConfig.HotKeySettings.ModifierKey1;
            cmbModifierKey2.SelectedItem = _appConfig.HotKeySettings.ModifierKey2;
            cmbAiSource.SelectedItem = _appConfig.GeneralSettings.AiSource.ToString();

            txtApiKey.Password = _appConfig.GeneralSettings.OpenAiSettings.ApiKey;
            ollamaHost.Text = _appConfig.GeneralSettings.OllamaSettings.Host;

            // We should check config 1st, and if it's empty or null, we should use fallback prompt
            txtPrompt.Text = string.IsNullOrEmpty(_appConfig.GeneralSettings.Prompt)
                ? _appConfig.GeneralSettings.Prompt
                : Prompts.WebViewLogAnalysisPrompt;
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred while initializing settings controls: {ex.Message}");
        }
    }

    private void BtnShowApiKey_Click(object sender, RoutedEventArgs e)
    {
        if (txtApiKey.PasswordRevealMode == PasswordRevealMode.Hidden)
        {
            txtApiKey.PasswordRevealMode = PasswordRevealMode.Visible;
            btnShowApiKey.Content = "Hide";
        }
        else
        {
            txtApiKey.PasswordRevealMode = PasswordRevealMode.Hidden;
            btnShowApiKey.Content = "Show";
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _appConfig.HotKeySettings.MainKey = cmbMainKey.SelectedItem.ToString();
            _appConfig.HotKeySettings.ModifierKey1 = cmbModifierKey1.SelectedItem.ToString();
            _appConfig.HotKeySettings.ModifierKey2 = cmbModifierKey2.SelectedItem.ToString();
            _appConfig.GeneralSettings.AiSource =
                Enum.Parse<AiSource>(cmbAiSource.SelectedItem.ToString() ?? throw new InvalidOperationException());

            _appConfig.GeneralSettings.OpenAiSettings.ApiKey = txtApiKey.Password;
            _appConfig.GeneralSettings.OllamaSettings.Host = ollamaHost.Text;
            _appConfig.GeneralSettings.Prompt = txtPrompt.Text;

            var settingsManager = App.Instance.Host.Services.GetRequiredService<SettingsManager>();
            settingsManager.SaveSettings(_appConfig);

            if (App.ChatWindow != null)
            {
                App.ChatWindow.Close();
            }

            App.Instance.HotKeyHandler.Dispose();
            App.Instance.HotKeyHandler = new HotKeyHandler(_appConfig);
            App.Instance.HotKeyHandler.HotKeyPressed += App.Instance.OnHotKeyPressed;

            await WindowHelper.ShowMessageDialogAsync(XamlRoot, "Settings saved", "Success");

            App.SettingsWindow.Close();
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred while saving settings: {ex.Message}");
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        App.SettingsWindow.Close();
    }
}