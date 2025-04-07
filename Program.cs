using System.Diagnostics;
using NAudio.CoreAudioApi;
using GlobalHotKeys;
using GlobalHotKeys.Native.Types;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ApplicationMuter;
using System.Text;
using System.Text.RegularExpressions;
using System.Management;
using System.Runtime.CompilerServices;

internal class Program
{
    private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

    private async Task MainAsync()
    {
#if !DEBUG
        HideConsoleWindow();
#endif

        Overlay h = null;

        using var hotKeyManager = new HotKeyManager();
        using var subscription = hotKeyManager.HotKeyPressed.Subscribe(HotKeyPressed);
        _ = hotKeyManager.Register(VirtualKeyCode.VK_F1, Modifiers.Control);
        _ = hotKeyManager.Register(VirtualKeyCode.VK_F2, Modifiers.Control);
        _ = hotKeyManager.Register(VirtualKeyCode.VK_F3, Modifiers.Control);

        new Task(async () =>
        {
            h = new Overlay();

            Application.EnableVisualStyles();
            Application.Run(h);
        }).Start();

        while (h is null)
            await Task.Delay(50);

        void HotKeyPressed(HotKey hotKey)
        {
            var hwnd = NativeMethods.GetForegroundWindow();
            _ = NativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
            var soundDevice = this.GetSoundDevice(processId);

            if (soundDevice != null)
            {
                void ShowNotification(int volumePercentage)
                {
                    var name = soundDevice.DisplayName;

                    if (string.IsNullOrWhiteSpace(name))
                        name = GetActiveWindowTitle(hwnd);

                    if (string.IsNullOrWhiteSpace(name))
                        name = "Unknown Application";

                    name = Regex.Replace(name, @"[^a-zA-Z0-9_ *.\-()]", "");

                    if (volumePercentage != -1)
                        h!.ShowNotification($"{name}\n{volumePercentage}%", name, processId, hwnd);
                    else
                        h!.ShowNotification($"{name}\n🔇", name, processId, hwnd);
                }

                if (hotKey.Key == VirtualKeyCode.VK_F1 && hotKey.Modifiers == Modifiers.Control)
                {
                    soundDevice.SimpleAudioVolume.Mute = !soundDevice.SimpleAudioVolume.Mute;

                    ShowNotification(soundDevice.SimpleAudioVolume.Mute ? -1 : (int)(Math.Round(soundDevice.SimpleAudioVolume.Volume, 2) * 100));
                }
                else if (hotKey.Key == VirtualKeyCode.VK_F2 && hotKey.Modifiers == Modifiers.Control)
                {
                    if (soundDevice.SimpleAudioVolume.Mute)
                        soundDevice.SimpleAudioVolume.Mute = false;

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

                    ShowNotification((int)(Math.Round(soundDevice.SimpleAudioVolume.Volume, 2) * 100));
                }
                else if (hotKey.Key == VirtualKeyCode.VK_F3 && hotKey.Modifiers == Modifiers.Control)
                {
                    if (soundDevice.SimpleAudioVolume.Mute)
                        soundDevice.SimpleAudioVolume.Mute = false;

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

                    ShowNotification((int)(Math.Round(soundDevice.SimpleAudioVolume.Volume, 2) * 100));
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

    private AudioSessionControl? GetSoundDevice(int processId)
    {
        using (var enumerator = new MMDeviceEnumerator())
        {
            var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

            for (int i = 0; i < defaultDevice.AudioSessionManager.Sessions.Count; i++)
            {
                var b = defaultDevice.AudioSessionManager.Sessions[i];

                if (b.GetProcessID == processId)
                    return b;
            }

            var deviceList = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in deviceList)
            {
                for (int i = 0; i < device.AudioSessionManager.Sessions.Count; i++)
                {
                    var b = device.AudioSessionManager.Sessions[i];

                    if (b.GetProcessID == processId)
                        return b;
                }
            }
        }

        return null;
    }

    public static string? GetActiveWindowTitle(nint handle)
    {
        const int nChars = 256;
        StringBuilder Buff = new StringBuilder(nChars);

        if (NativeMethods.GetWindowText(handle, Buff, nChars) > 0)
        {
            return Buff.ToString();
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetProcessName(int pid)
    {
        var processHandle = NativeMethods.OpenProcess(0x1000, false, pid);

        if (processHandle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            const int lengthSb = 4000;

            var sb = new StringBuilder(lengthSb);

            string? result = null;

            if (NativeMethods.GetModuleFileNameEx(processHandle, IntPtr.Zero, sb, lengthSb) > 0)
                result = Path.GetFullPath(sb.ToString());

            return result;
        }
        finally
        {
            NativeMethods.CloseHandle(processHandle);
        }
    }
}

public static class NativeMethods
{
    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    [DllImport("psapi.dll")]
    public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In][MarshalAs(UnmanagedType.U4)] int nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;   
        public int Top;    
        public int Right;   
        public int Bottom;  
    }

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

    [DllImport("user32.dll")]
    internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern bool IsWindowVisible(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("user32.dll")]
    internal static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
}