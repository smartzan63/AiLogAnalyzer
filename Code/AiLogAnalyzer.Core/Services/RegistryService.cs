namespace AiLogAnalyzer.Core.Services;

using System.Runtime.Versioning;
using Microsoft.Win32;

[SupportedOSPlatform("windows")]
public class RegistryService : IRegistryService
{
    [SupportedOSPlatform("windows")]
    public virtual string GetMachineGuid()
    {
        const string key = @"SOFTWARE\Microsoft\Cryptography";
        const string valueName = "MachineGuid";

        using var localMachineX64View = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var registryKey = localMachineX64View.OpenSubKey(key);

        var machineGuidObj = registryKey?.GetValue(valueName);

        return machineGuidObj?.ToString() ?? string.Empty;
    }
}