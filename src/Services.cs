using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace IPAClipboard;

public static class Catalog
{
    public static IReadOnlyList<SymbolEntry> All { get; } = Load();
    private static IReadOnlyList<SymbolEntry> Load()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("IPAClipboard.Data.catalog.json") ?? throw new InvalidDataException("未找到音标数据");
        return JsonSerializer.Deserialize<List<SymbolEntry>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }
    public static string Format(SymbolEntry entry, CopyFormat format) => !entry.Wrap ? entry.Text : format switch
    {
        CopyFormat.Slashes => $"/{entry.Text}/", CopyFormat.Brackets => $"[{entry.Text}]", _ => entry.Text
    };
}

public sealed class SettingsStore(string? directory = null)
{
    public string DirectoryPath { get; } = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IPAClipboard");
    public string FilePath => Path.Combine(DirectoryPath, "settings.json");
    public UserSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new();
            var s = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(FilePath)) ?? new();
            s.Width = Safe(s.Width, 440, 300, 1400); s.Height = Safe(s.Height, 570, 360, 1400);
            s.MiniWidth = Safe(s.MiniWidth, 320, 300, 1000); s.MiniHeight = Safe(s.MiniHeight, 420, 300, 1200);
            if (s.Left is { } left && !double.IsFinite(left)) s.Left = null;
            if (s.Top is { } top && !double.IsFinite(top)) s.Top = null;
            if (!new[] { "english", "ipa", "common", "saved" }.Contains(s.Page)) s.Page = "english";
            s.Category ??= "全部分类";
            if (!Enum.IsDefined(s.Format)) s.Format = CopyFormat.Plain;
            var ids = Catalog.All.Select(x => x.Id).ToHashSet();
            s.Favorites = (s.Favorites ?? []).Where(ids.Contains).Distinct().ToList();
            s.Recent = (s.Recent ?? []).Where(ids.Contains).Distinct().Take(12).ToList();
            return s;
        }
        catch { return new(); }
    }
    private static double Safe(double value, double fallback, double min, double max) => double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
    public bool Save(UserSettings settings)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
            File.Move(temp, FilePath, true);
            return true;
        }
        catch { return false; }
    }
}

public static class ClipboardService
{
    // Supply immediate CF_UNICODETEXT storage owned by Windows, so it survives process exit.
    // Native OpenClipboard attempts return immediately; retry delays yield to the UI dispatcher.
    public static async Task<bool> CopyAsync(string text, Func<string, bool>? writer = null)
    {
        for (int attempt = 0; attempt < 6; attempt++)
        {
            try
            {
                if (writer != null) { if (writer(text)) return true; }
                else
                {
                    if (TryWriteUnicode(text) && ReadUnicode() == text) return true;
                }
            }
            catch (ExternalException) { }
            catch (InvalidOperationException) { }
            if (attempt < 5) await Task.Delay(35 * (attempt + 1));
        }
        return false;
    }
    private static bool TryWriteUnicode(string text)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(text + '\0');
        IntPtr memory = GlobalAlloc(0x0002, (UIntPtr)bytes.Length);
        if (memory == IntPtr.Zero) return false;
        bool opened = false;
        try
        {
            var pointer = GlobalLock(memory); if (pointer == IntPtr.Zero) return false;
            try { Marshal.Copy(bytes, 0, pointer, bytes.Length); } finally { GlobalUnlock(memory); }
            var owner = new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow).Handle;
            opened = OpenClipboard(owner); if (!opened || !EmptyClipboard()) return false;
            if (SetClipboardData(13, memory) == IntPtr.Zero) return false;
            memory = IntPtr.Zero; // Ownership transferred to the system.
            return true;
        }
        finally { if (opened) CloseClipboard(); if (memory != IntPtr.Zero) GlobalFree(memory); }
    }
    public static string? ReadUnicode()
    {
        var owner = new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow).Handle;
        if (!OpenClipboard(owner)) return null;
        try
        {
            var memory = GetClipboardData(13); if (memory == IntPtr.Zero) return null;
            var pointer = GlobalLock(memory); if (pointer == IntPtr.Zero) return null;
            try { return Marshal.PtrToStringUni(pointer); } finally { GlobalUnlock(memory); }
        }
        finally { CloseClipboard(); }
    }
    [DllImport("user32.dll", SetLastError = true)] private static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] private static extern bool CloseClipboard();
    [DllImport("user32.dll")] private static extern bool EmptyClipboard();
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetClipboardData(uint format, IntPtr memory);
    [DllImport("user32.dll")] private static extern IntPtr GetClipboardData(uint format);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalAlloc(uint flags, UIntPtr size);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalLock(IntPtr memory);
    [DllImport("kernel32.dll")] private static extern bool GlobalUnlock(IntPtr memory);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalFree(IntPtr memory);
}

public sealed class UsageHistory(UserSettings settings)
{
    public void Record(SymbolEntry entry)
    {
        settings.Recent.Remove(entry.Id); settings.Recent.Insert(0, entry.Id);
        if (settings.Recent.Count > 12) settings.Recent.RemoveRange(12, settings.Recent.Count - 12);
    }
    public void ToggleFavorite(SymbolEntry entry)
    {
        if (!settings.Favorites.Remove(entry.Id)) settings.Favorites.Add(entry.Id);
    }
}
