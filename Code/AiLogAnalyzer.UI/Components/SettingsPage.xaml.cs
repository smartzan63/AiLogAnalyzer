using System;
using System.Linq;
using Windows.System;
using AiLogAnalyzer.Core;
using AiLogAnalyzer.Core.Configuration;
using AiLogAnalyzer.UI.Managers;
using AiLogAnalyzer.UI.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AiLogAnalyzer.UI.Components;

//TODO: Add settings for Ollama, now it is hardcoded or missing
public sealed partial class SettingsPage : Page
{
    private readonly AppConfig _appConfig;

    public SettingsPage(AppConfig appConfig)
    {
        this.InitializeComponent();
        _appConfig = appConfig;
        InitializeSettingsControls();
    }

    private void InitializeSettingsControls()
    {
        try
        {
            var listOfVirtualKeys = Enum.GetValues(typeof(VirtualKey)).Cast<VirtualKey>().Select(vk => vk.ToString())
                .ToList();
            var aiSources = Enum.GetValues(typeof(AiSource)).Cast<AiSource>().Select(source => source.ToString()).ToList();


            cmbMainKey.ItemsSource = listOfVirtualKeys;
            cmbModifierKey1.ItemsSource = listOfVirtualKeys;
            cmbModifierKey2.ItemsSource = listOfVirtualKeys;
            cmbAiSource.ItemsSource = aiSources;

            cmbMainKey.SelectedItem = _appConfig.HotKeySettings.MainKey;
            cmbModifierKey1.SelectedItem = _appConfig.HotKeySettings.ModifierKey1;
            cmbModifierKey2.SelectedItem = _appConfig.HotKeySettings.ModifierKey2;
            cmbAiSource.SelectedItem = _appConfig.GeneralSettings.AiSource.ToString();

            txtApiKey.Password = _appConfig.GeneralSettings.OpenAiSettings.ApiKey;
            txtPrompt.Text = string.IsNullOrEmpty(_appConfig.GeneralSettings.Prompt)
                ? Prompts.WebViewLogAnalysisPrompt
                : _appConfig.GeneralSettings.Prompt;
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
            _appConfig.GeneralSettings.AiSource = Enum.Parse<AiSource>(cmbAiSource.SelectedItem.ToString() ?? throw new InvalidOperationException() );

            _appConfig.GeneralSettings.OpenAiSettings.ApiKey = txtApiKey.Password;
            _appConfig.GeneralSettings.Prompt = txtPrompt.Text;

            var settingsManager = UI.App.Instance.Host.Services.GetRequiredService<SettingsManager>();
            settingsManager.SaveSettings(_appConfig);
            
            if (UI.App.ChatWindow != null)
            {
                UI.App.ChatWindow.Close();
            }
            
            // settingsManager.UpdateAiService(UI.App.Instance.Host.Services, UI.App.ChatWindow, _appConfig.GeneralSettings.AiSource);
            
            UI.App.Instance.HotKeyHandler.Dispose();
            UI.App.Instance.HotKeyHandler = new HotKeyHandler(_appConfig);
            UI.App.Instance.HotKeyHandler.HotKeyPressed += UI.App.Instance.OnHotKeyPressed;
            
            await WindowHelper.ShowMessageDialogAsync(this.XamlRoot,"Settings saved", "Success");
            
            UI.App.SettingsWindow.Close();

        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred while saving settings: {ex.Message}");
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        UI.App.SettingsWindow.Close();
    }
}