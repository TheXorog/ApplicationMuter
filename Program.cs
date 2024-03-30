using System.Diagnostics;
using NAudio.CoreAudioApi;
using GlobalHotKeys;
using GlobalHotKeys.Native.Types;
using System.Runtime.InteropServices;
using Microsoft.Toolkit.Uwp.Notifications;

internal class Program
{
    private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

    private async Task MainAsync()
    {
        HideConsoleWindow();

        using var hotKeyManager = new HotKeyManager();
        using var subscription = hotKeyManager.HotKeyPressed.Subscribe(HotKeyPressed);
        _ = hotKeyManager.Register(VirtualKeyCode.VK_F1, Modifiers.Control);
        _ = hotKeyManager.Register(VirtualKeyCode.VK_F2, Modifiers.Control);
        _ = hotKeyManager.Register(VirtualKeyCode.VK_F3, Modifiers.Control);

        void HotKeyPressed(HotKey hotKey)
        {
            var processId = this.GetActiveWindowProcessId();
            var soundDevice = this.GetSoundDevice(processId);

            if (soundDevice != null)
            {
                if (hotKey.Key == VirtualKeyCode.VK_F1 && hotKey.Modifiers == Modifiers.Control)
                {

                    soundDevice.SimpleAudioVolume.Mute = !soundDevice.SimpleAudioVolume.Mute;

                    new ToastContentBuilder()
                    .SetToastScenario(ToastScenario.Default)
                    .SetToastDuration(ToastDuration.Short)
                    .AddText($"ℹ️ {(soundDevice.SimpleAudioVolume.Mute ? "Muted" : "Unmuted")} '{soundDevice.DisplayName}'")
                    .Show();
                }
                else if (hotKey.Key == VirtualKeyCode.VK_F2 && hotKey.Modifiers == Modifiers.Control)
                {
                    if (soundDevice.SimpleAudioVolume.Volume - 0.05f >= 0)
                    {
                        Console.WriteLine(soundDevice.SimpleAudioVolume.Volume - 0.05f);
                        soundDevice.SimpleAudioVolume.Volume = soundDevice.SimpleAudioVolume.Volume - 0.05f;
                    }
                    else
                    {
                        Console.WriteLine(0f);
                        soundDevice.SimpleAudioVolume.Volume = 0f;
                    }
                }
                else if (hotKey.Key == VirtualKeyCode.VK_F3 && hotKey.Modifiers == Modifiers.Control)
                {
                    if (soundDevice.SimpleAudioVolume.Volume + 0.05f <= 1)
                    {
                        Console.WriteLine(soundDevice.SimpleAudioVolume.Volume + 0.05f);
                        soundDevice.SimpleAudioVolume.Volume = soundDevice.SimpleAudioVolume.Volume + 0.05f;
                    }
                    else
                    {
                        Console.WriteLine(1f);
                        soundDevice.SimpleAudioVolume.Volume = 1f;
                    }
                }
            }
        }

        await Task.Delay(-1);
    }

    private static void HideConsoleWindow()
    {
        _ = NativeMethods.FreeConsole();
        IntPtr hWnd = NativeMethods.GetConsoleWindow();
        _ = NativeMethods.ShowWindow(hWnd, NativeMethods.SW_HIDE);
    }

    private int GetActiveWindowProcessId()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        _ = NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
        return processId;
    }

    private AudioSessionControl? GetSoundDevice(int processId)
    {
        using (var enumerator = new MMDeviceEnumerator())
        {
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                var sessions = new List<AudioSessionControl>();
                for (int i = 0; i < device.AudioSessionManager.Sessions.Count; i++)
                {
                    sessions.Add(device.AudioSessionManager.Sessions[i]);
                }

                foreach (var b in sessions)
                {
                    if (b.GetProcessID == processId)
                        return b;
                }
            }
        }

        return null;
    }
}

internal static class NativeMethods
{
    [DllImport("kernel32.dll")]
    internal static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    internal static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    internal static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    internal const int SW_HIDE = 0;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
}