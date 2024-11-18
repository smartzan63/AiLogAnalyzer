namespace AiLogAnalyzer.UI.Utility;

using System;
using System.Runtime.InteropServices;
using Windows.System;
using Core.Configuration;
using Core;

public sealed partial class HotKeyHandler : IDisposable
{
    public event Action HotKeyPressed;

    private readonly IntPtr _hookId;
    private readonly AppConfig _config;
    private readonly LowLevelKeyboardProc _proc;
    private bool _keyPressed;

    // P/Invoke declarations
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    public HotKeyHandler(AppConfig config)
    {
        _config = config;
        _proc = HookCallback;
        _hookId = SetHook(_proc);
    }

    public void Dispose()
    {
        UnhookWindowsHookEx(_hookId);
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
            GetModuleHandle(curModule?.ModuleName ?? throw new InvalidOperationException()), 0);
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            if (wParam == (IntPtr)WM_KEYDOWN)
            {
                HandleKeyDown(vkCode);
            }
            else if (wParam == (IntPtr)WM_KEYUP)
            {
                HandleKeyUp(vkCode);
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void HandleKeyDown(int vkCode)
    {
        if (IsHotKeysPressed((VirtualKey)vkCode) && !_keyPressed)
        {
            _keyPressed = true;
            Log.Debug("Hot keys pressed");
            OnHotKeyPressed();
        }
    }

    private void HandleKeyUp(int vkCode)
    {
        var hotKeySettings = _config.HotKeySettings;

        if ((VirtualKey)vkCode == ParseVirtualKey(hotKeySettings.MainKey) ||
            (VirtualKey)vkCode == ParseVirtualKey(hotKeySettings.ModifierKey1) ||
            (VirtualKey)vkCode == ParseVirtualKey(hotKeySettings.ModifierKey2))
        {
            _keyPressed = false;
        }
    }

    private bool IsHotKeysPressed(VirtualKey key)
    {
        var hotKeySettings = _config.HotKeySettings;

        if (hotKeySettings == null)
        {
            throw new InvalidOperationException("HotKeySettings is not initialized.");
        }
        
        var mainKeyPressed = key == ParseVirtualKey(hotKeySettings.MainKey);
        var modifier1Pressed = GetAsyncKeyState((int)ParseVirtualKey(hotKeySettings.ModifierKey1)) < 0;
        var modifier2Pressed = GetAsyncKeyState((int)ParseVirtualKey(hotKeySettings.ModifierKey2)) < 0;


        // Log.Debug($"Key states - Main: {mainKeyPressed}, Mod1: {modifier1Pressed}, Mod2: {modifier2Pressed}");

        return mainKeyPressed && modifier1Pressed && modifier2Pressed;
    }


    private void OnHotKeyPressed()
    {
        HotKeyPressed?.Invoke();
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
    
    private VirtualKey ParseVirtualKey(string key)
    {
        return (VirtualKey)Enum.Parse(typeof(VirtualKey), key);
    }
}