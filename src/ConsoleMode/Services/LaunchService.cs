using System.Diagnostics;
using ConsoleMode.Models;
using ConsoleMode.Native;
using Microsoft.Win32;

namespace ConsoleMode.Services;

public sealed class LaunchService
{
    public void StartPlaynite()
    {
        var exe = GetPlaynitePath();
        if (exe is not null)
        {
            ProcessRunner.StartDetached(exe);
            return;
        }
        ProcessRunner.StartDetached("playnite://playnite/start");
    }

    /// <summary>"customApp": starts the exe from its own folder (emulators look for their files there).</summary>
    /// <returns>The started process's id, to follow a hand-off from it; null if Windows didn't say.</returns>
    public int? StartCustomApp(string exePath, string? arguments)
    {
        var exe = CustomApp.CleanPath(exePath);
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            Arguments = arguments?.Trim() ?? "",
            WorkingDirectory = Path.GetDirectoryName(exe) ?? "",
            UseShellExecute = true
        });
        try { return process?.Id; }
        catch (InvalidOperationException) { return null; }
    }

    /// <summary>
    /// Asks the open JoyChromium to leave full screen: a second run hands its arguments to the open
    /// instance and exits at once, so this doesn't open another browser.
    /// </summary>
    public void ReturnJoyChromiumToWindow()
    {
        if (GetJoyChromiumPath() is not { } exe) return;
        StartCustomApp(exe, JoyChromium.WindowArgument);
    }

    /// <summary>The installed JoyChromium.exe: the per-user install folder, else the uninstall key's icon path.</summary>
    public static string? GetJoyChromiumPath()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string? displayIcon = null;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(JoyChromium.UninstallKey);
            displayIcon = key?.GetValue("DisplayIcon") as string;
        }
        catch { /* registry optional */ }
        return JoyChromium.Resolve(string.IsNullOrWhiteSpace(local) ? null : JoyChromium.DefaultPath(local), displayIcon, File.Exists);
    }

    /// <summary>The exe's FileDescription ("RetroArch"), else its file name.</summary>
    public static string GetCustomAppName(string exePath)
    {
        string? description = null;
        try
        {
            var exe = CustomApp.CleanPath(exePath);
            if (File.Exists(exe)) description = FileVersionInfo.GetVersionInfo(exe).FileDescription;
        }
        catch { /* no version info: fall back to the file name */ }
        return CustomApp.DisplayName(exePath, description);
    }

    public static bool IsProcessRunning(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;
        var processes = Process.GetProcessesByName(processName);
        foreach (var p in processes) p.Dispose();
        return processes.Length > 0;
    }

    /// <summary>
    /// The processes' main windows, largest first. Windows may report a tooltip as an app's "main" window
    /// (seen with JoyChromium), so the first one is the one most likely to be the app itself.
    /// </summary>
    public static nint[] GetProcessWindows(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return [];
        var handles = new List<nint>();
        foreach (var p in Process.GetProcessesByName(processName))
        {
            using (p)
            {
                if (p.MainWindowHandle != 0) handles.Add(p.MainWindowHandle);
            }
        }
        return [.. handles.OrderByDescending(NativeWindows.GetWindowArea)];
    }

    public bool IsPlayniteAvailable() => GetPlaynitePath() is not null;

    /// <summary>User-chosen Playnite folder or exe (portable installs); tried before auto-detection.</summary>
    public string CustomPlaynitePath { get; set; } = "";

    public string? GetPlaynitePath()
    {
        if (PlaynitePaths.ResolveCustom(CustomPlaynitePath) is { } custom) return custom;

        var candidates = new List<string>();
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(local))
            candidates.Add(Path.Combine(local, "Playnite", "Playnite.FullscreenApp.exe"));
        candidates.Add(@"C:\Program Files\Playnite\Playnite.FullscreenApp.exe");
        candidates.Add(@"C:\Program Files (x86)\Playnite\Playnite.FullscreenApp.exe");

        foreach (var path in candidates)
        {
            if (File.Exists(path)) return path;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Playnite_is1");
            var install = key?.GetValue("InstallLocation") as string;
            if (!string.IsNullOrWhiteSpace(install))
            {
                var exe = Path.Combine(install, "Playnite.FullscreenApp.exe");
                if (File.Exists(exe)) return exe;
            }
        }
        catch { /* registry optional */ }

        return null;
    }

    public nint[] GetPlayniteHandles()
    {
        return Process.GetProcessesByName("Playnite.FullscreenApp")
            .Where(p => p.MainWindowHandle != 0)
            .Select(p => p.MainWindowHandle)
            .ToArray();
    }

    public bool IsPlayniteRunning() => Process.GetProcessesByName("Playnite.FullscreenApp").Length > 0;

    /// <summary>
    /// Playnite puts its window away while a game runs ("Minimize after game startup" in its fullscreen settings, on in
    /// themes like Selene): the same window is hidden (or minimized) and comes back when the game ends. It still
    /// exists, unlike a loading window swapped for the main one, which is destroyed.
    /// </summary>
    public static bool IsPlayniteWindowPutAway(nint window) =>
        window != 0 && NativeWindows.IsWindow(window)
                    && (!NativeWindows.IsWindowVisible(window) || NativeWindows.IsIconic(window));

    public nint[] GetFullscreenHandles(string mode) =>
        mode == "playnite" ? GetPlayniteHandles() : [];

    public bool IsPlayniteActive(ConsoleRuntimeState state)
    {
        if (state.CachedBigPictureHandle != 0)
        {
            if (NativeWindows.IsWindowStillVisible(state.CachedBigPictureHandle) &&
                NativeWindows.GetWindowArea(state.CachedBigPictureHandle) > 200000)
            {
                return true;
            }
            state.CachedBigPictureHandle = 0;
        }

        foreach (var h in GetPlayniteHandles())
        {
            if (NativeWindows.GetWindowArea(h) <= 200000) continue;
            state.CachedBigPictureHandle = h;
            return true;
        }
        return false;
    }

    public bool IsFullscreenActive(string mode, ConsoleRuntimeState state) => mode switch
    {
        "playnite" => IsPlayniteActive(state),
        _ => false
    };
}
