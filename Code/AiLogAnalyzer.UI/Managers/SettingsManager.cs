﻿namespace AiLogAnalyzer.UI.Managers;

using Core.Configuration;
using Core.Services;
using Utility;

// TODO:
// BUG: When OpenAI key is set for the first time, it's not propagated to the OpenAI service, so app needs to be restarted
// RETEST IS NEEDED
// Bug When OpenAI key is set as unencrypted it got decrypted incorrectly

public class SettingsManager(IConfigurationService configurationService, LogAnalysisServiceProvider logAnalysisServiceProvider)
{
    public AppConfig LoadSettings(bool forceReload = false)
    {
        return configurationService.LoadSettings(forceReload);
    }

    public void SaveSettings(AppConfig settings)
    {
        configurationService.SaveSettings(settings);
        logAnalysisServiceProvider.UpdateConfig();
            
        App.Instance.HotKeyHandler?.Dispose();
        App.Instance.HotKeyHandler = new HotKeyHandler(settings);
    }
}