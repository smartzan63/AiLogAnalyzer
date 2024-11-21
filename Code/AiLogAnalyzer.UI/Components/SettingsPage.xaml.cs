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
            var aiSources = Enum.GetValues(typeof(AiSource)).Cast<AiSource>().Select(source => source.ToString())
                .ToList();
            cmbAiSource.ItemsSource = aiSources;
            cmbAiSource.SelectedItem = _appConfig.GeneralSettings.AiSource.ToString();

            txtPrompt.Text = string.IsNullOrEmpty(_appConfig.GeneralSettings.Prompt)
                ? Prompts.WebViewLogAnalysisPrompt
                : _appConfig.GeneralSettings.Prompt;

            txtMaxTokens.Text = _appConfig.GeneralSettings.MaxTokens.ToString();
            txtTemperature.Text = _appConfig.GeneralSettings.Temperature.ToString();

            txtApiKey.Password = _appConfig.GeneralSettings.OpenAiSettings.ApiKey;

            cmbOpenAiModel.ItemsSource = OpenAiSettings.ModelList?.Distinct().ToList(); // Убираем дублирование
            cmbOpenAiModel.SelectedItem = _appConfig.GeneralSettings.OpenAiSettings.CurrentModel;

            ollamaHost.Text = _appConfig.GeneralSettings.OllamaSettings.Host;

            cmbOllamaModel.ItemsSource = OllamaSettings.ModelList?.Distinct().ToList(); // Убираем дублирование
            cmbOllamaModel.SelectedItem = _appConfig.GeneralSettings.OllamaSettings.CurrentModel;

            var listOfVirtualKeys = Enum.GetValues(typeof(VirtualKey))
                .Cast<VirtualKey>()
                .Select(vk => vk.ToString())
                .ToList();

            cmbModifierKey1.ItemsSource = listOfVirtualKeys;
            cmbModifierKey2.ItemsSource = listOfVirtualKeys.Prepend("None").ToList();
            cmbMainKey.ItemsSource = listOfVirtualKeys;

            cmbModifierKey1.SelectedItem = _appConfig.HotKeySettings.ModifierKey1;
            cmbModifierKey2.SelectedItem = string.IsNullOrEmpty(_appConfig.HotKeySettings.ModifierKey2)
                ? "None"
                : _appConfig.HotKeySettings.ModifierKey2;
            cmbMainKey.SelectedItem = _appConfig.HotKeySettings.MainKey;

            cmbSettingsSelector.SelectedIndex = 0;
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
            // General settings
            if (cmbAiSource.SelectedItem != null)
            {
                _appConfig.GeneralSettings.AiSource = Enum.Parse<AiSource>(cmbAiSource.SelectedItem.ToString());
            }

            _appConfig.GeneralSettings.Prompt = txtPrompt.Text;
            _appConfig.GeneralSettings.MaxTokens = int.TryParse(txtMaxTokens.Text, out int maxTokens)
                ? maxTokens
                : _appConfig.GeneralSettings.MaxTokens;
            _appConfig.GeneralSettings.Temperature = float.TryParse(txtTemperature.Text, out float temperature)
                ? temperature
                : _appConfig.GeneralSettings.Temperature;

            // OpenAI Settings
            _appConfig.GeneralSettings.OpenAiSettings.ApiKey = txtApiKey.Password;
            if (cmbOpenAiModel.SelectedItem != null)
            {
                _appConfig.GeneralSettings.OpenAiSettings.CurrentModel = cmbOpenAiModel.SelectedItem.ToString();
            }

            // Ollama settings
            _appConfig.GeneralSettings.OllamaSettings.Host = ollamaHost.Text;
            if (cmbOllamaModel.SelectedItem != null)
            {
                _appConfig.GeneralSettings.OllamaSettings.CurrentModel = cmbOllamaModel.SelectedItem.ToString();
            }

            // Saving HotKe settings
            _appConfig.HotKeySettings.ModifierKey1 = cmbModifierKey1.SelectedItem?.ToString();
            _appConfig.HotKeySettings.ModifierKey2 = cmbModifierKey2.SelectedItem?.ToString() == "None"
                ? null
                : cmbModifierKey2.SelectedItem?.ToString();
            _appConfig.HotKeySettings.MainKey = cmbMainKey.SelectedItem?.ToString();

            // Saving via SettingsManager
            var settingsManager = App.Instance.Host.Services.GetRequiredService<SettingsManager>();
            settingsManager.SaveSettings(_appConfig);

            if (App.ChatWindow != null)
            {
                App.ChatWindow.Close();
            }

            // Reinitializing the hotkeys handler
            App.Instance.HotKeyHandler.Dispose();
            App.Instance.HotKeyHandler = new HotKeyHandler(_appConfig);
            App.Instance.HotKeyHandler.HotKeyPressed += App.Instance.OnHotKeyPressed;

            await WindowHelper.ShowMessageDialogAsync(XamlRoot, "Settings saved successfully.", "Success");

            App.SettingsWindow.Close();
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred while saving settings: {ex.Message}");
            await WindowHelper.ShowMessageDialogAsync(XamlRoot, $"Error saving settings: {ex.Message}", "Error");
        }
    }


    private void SettingsSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbSettingsSelector.SelectedItem is not ComboBoxItem selectedItem)
            return;

        GeneralSettingsGrid.Visibility = Visibility.Collapsed;
        OpenAISettingsGrid.Visibility = Visibility.Collapsed;
        OllamaSettingsGrid.Visibility = Visibility.Collapsed;
        HotKeySettingsGrid.Visibility = Visibility.Collapsed;

        switch (selectedItem.Content.ToString())
        {
            case "General":
                GeneralSettingsGrid.Visibility = Visibility.Visible;
                break;
            case "OpenAI":
                OpenAISettingsGrid.Visibility = Visibility.Visible;
                break;
            case "Ollama":
                OllamaSettingsGrid.Visibility = Visibility.Visible;
                break;
            case "HotKeys":
                HotKeySettingsGrid.Visibility = Visibility.Visible;
                break;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        App.SettingsWindow.Close();
    }
}