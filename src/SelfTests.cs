using System.IO;
using System.Text.Json;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace IPAClipboard;

// Opt-in diagnostics exercised against the very same self-contained release executable.
// Normal double-click startup never runs this path; test preferences stay in the supplied output folder.
internal static class SelfTests
{
    public static async Task RunAsync(MainWindow window, string output)
    {
        Directory.CreateDirectory(output);
        var checks = new List<string>();
        void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); checks.Add(name); }
        var all = Catalog.All;
        Check(all.Count(x => x.Page == "english") == 48, "英语教材48项完整");
        Check(all.Count(x => x.Page == "english" && x.Group == "单元音") == 12, "单元音12项");
        Check(all.Count(x => x.Page == "english" && x.Group == "双元音") == 8, "双元音8项");
        Check(all.Count(x => x.Page == "english" && x.Group.StartsWith("教材扩展")) == 4, "教学组合4项独立标注");
        Check(all.Select(x => x.Id).Distinct().Count() == all.Count, "结构化数据ID无重复");
        Check(all.All(x => !x.Text.Contains('◌') && !x.Text.Contains(':') && !x.Text.Contains('\uFFFD')), "复制数据不含占位圆圈、ASCII冒号或替代字符");
        foreach (string symbol in new[] { "θ", "ð", "ʃ", "ʒ", "ŋ", "ɜː", "aɪ" })
        {
            var entry = all.First(x => x.Page == "english" && x.Text == symbol);
            Check(await window.CopyEntryAsync(new(entry, false, false)), $"实际剪贴板写入 {symbol}");
            string? actual = await ReadClipboardAsync();
            Check(actual == symbol, $"实际CF_UNICODETEXT回读 {symbol} (实际={actual})");
            await Task.Delay(100);
        }
        var ai = all.First(x => x.Text == "aɪ");
        foreach (var format in Enum.GetValues<CopyFormat>())
        {
            window.FormatBox.SelectedIndex = (int)format;
            bool copied = await window.CopyEntryAsync(new(ai, false, false));
            string? actual = await ReadClipboardAsync();
            Check(copied && actual == (format switch { CopyFormat.Slashes => "/aɪ/", CopyFormat.Brackets => "[aɪ]", _ => "aɪ" }), $"复制格式 {format} (实际={actual}, 选项={window.FormatBox.SelectedIndex}, 设置={window.Settings.Format}, 成功={copied})");
            await Task.Delay(100);
        }
        var nasal = all.First(x => x.Text == "\u0303");
        await window.CopyEntryAsync(new(nasal, false, false));
        Check(await ReadClipboardAsync() == "\u0303", "组合鼻化符保持U+0303，括号与圆圈均不复制");
        Check(Catalog.Format(all.First(x => x.Text == "ʰ"), CopyFormat.Brackets) == "ʰ", "送气修饰符不加括号");
        int attempts = 0;
        Check(await ClipboardService.CopyAsync("retry", _ => ++attempts == 3) && attempts == 3, "剪贴板暂占重试后成功");
        attempts = 0;
        Check(!await ClipboardService.CopyAsync("busy", _ => { attempts++; return false; }) && attempts == 6, "剪贴板六次失败返回失败");
        window.FormatBox.SelectedIndex = 0;
        window.EnglishTab.IsChecked = true;
        window.SearchBox.Text = "ship";
        Check(window.VisibleGroups.SelectMany(g => g.Items).Single().Entry.Text == "ʃ", "英文示例实时搜索");
        window.SearchBox.Text = "鼻音";
        Check(window.VisibleGroups.Sum(g => g.Items.Count) == 3, "分类与中文名称搜索");
        window.SearchBox.Text = "ɜː";
        Check(window.VisibleGroups.Sum(g => g.Items.Count) == 1, "Unicode音标实时搜索");
        window.SearchBox.Clear();
        window.IpaTab.IsChecked = true;
        Check(window.VisibleGroups.Count == 7, "完整IPA七大分类");
        window.CategoryBox.SelectedItem = "元音 · Vowels";
        Check(window.VisibleGroups.Sum(g => g.Items.Count) == 28, "完整IPA元音28项筛选");
        window.CommonTab.IsChecked = true;
        Check(window.VisibleGroups.Sum(g => g.Items.Count) == 9, "常用符号9项");
        var settings = new UserSettings(); var history = new UsageHistory(settings);
        foreach (var entry in all.Take(15)) history.Record(entry);
        history.Record(all[2]);
        Check(settings.Recent.Count == 12 && settings.Recent[0] == all[2].Id && settings.Recent.Distinct().Count() == 12, "最近12项去重与排序");
        history.ToggleFavorite(ai); Check(settings.Favorites.Contains(ai.Id), "添加收藏");
        history.ToggleFavorite(ai); Check(settings.Favorites.Count == 0, "取消收藏");
        var testStore = new SettingsStore(Path.Combine(output, "roundtrip"));
        settings.Mini = true; settings.Topmost = false; settings.Page = "ipa"; settings.Category = "元音 · Vowels"; settings.Format = CopyFormat.Brackets; settings.Left = -1000;
        Check(testStore.Save(settings), "配置保存");
        var loaded = testStore.Load();
        Check(loaded.Mini && !loaded.Topmost && loaded.Page == "ipa" && loaded.Format == CopyFormat.Brackets && loaded.Recent.SequenceEqual(settings.Recent) && loaded.Left == -1000, "配置完整恢复");
        File.WriteAllText(testStore.FilePath, "{ corrupt");
        Check(testStore.Load().Topmost && testStore.Load().Width == 440, "损坏配置安全恢复默认");
        File.Delete(testStore.FilePath);
        Check(testStore.Load().Format == CopyFormat.Plain, "配置丢失恢复默认");
        // Verify every displayed code point has a glyph in the built-in font fallback chain.
        var glyphs = new[] { "Cambria", "Segoe UI", "Segoe UI Symbol", "Arial", "Microsoft YaHei UI" }
            .Select(f => new Typeface(f)).Select(t => t.TryGetGlyphTypeface(out var glyph) ? glyph : null).OfType<GlyphTypeface>().ToList();
        var missing = all.SelectMany(x => x.Display.EnumerateRunes()).Select(x => x.Value).Distinct().Where(cp => !glyphs.Any(g => g.CharacterToGlyphMap.ContainsKey(cp))).ToArray();
        Check(missing.Length == 0, "内置字体覆盖全部音标码位: " + string.Join(",", missing.Select(x => $"U+{x:X4}")));
        window.EnglishTab.IsChecked = true; window.SearchBox.Clear();
        window.Topmost = false;
        var pin = window.PinButton; pin.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Check(window.Topmost, "置顶按钮开启"); pin.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Check(!window.Topmost, "置顶按钮关闭");
        window.Width = 440; window.Height = 570;
        await Settle();
        var hwnd = new WindowInteropHelper(window).Handle;
        int Hit(double x, double y)
        {
            var point = window.PointToScreen(new Point(x, y));
            int packed = ((int)point.Y << 16) | ((int)point.X & 0xffff);
            return (int)SendMessage(hwnd, 0x0084, IntPtr.Zero, new IntPtr(packed));
        }
        Check(Hit(120, 25) == 2, "标题文字区域返回原生HTCAPTION，支持拖动");
        Check(Hit(245, 25) == 2, "标题空白区域返回原生HTCAPTION，支持拖动");
        Check(Hit(320, 25) == 1 && Hit(350, 25) == 1, "标题工具按钮返回HTCLIENT，仍可点击");
        Check(Hit(160, 70) == 1, "搜索区返回HTCLIENT，不拦截输入");
        foreach (var scale in new[] { 1.0, 1.25, 1.5, 2.0 }) Render(window, Path.Combine(output, $"normal-{scale:0.##}.png"), scale);
        checks.Add("正常窗口按100/125/150/200%渲染输出（非实际显示器DPI切换）");
        window.SwitchMini(); await Settle();
        Check(window.Settings.Mini && window.Width == 320, "迷你模式尺寸与状态");
        Render(window, Path.Combine(output, "mini.png"), 1);
        window.SwitchMini(); await Settle(); Check(!window.Settings.Mini && window.Width == 440, "完整模式恢复尺寸");
        window.Width = 300; window.Height = 360; await Settle(); Render(window, Path.Combine(output, "minimum.png"), 1);
        Check(window.SymbolScroll.ScrollableHeight > 0, "最小尺寸音标区仍可纵向滚动");
        window.Width = 440; window.Height = 570;
        window.Left = -30000; window.Top = -30000; window.EnsureOnScreen();
        Check(window.Left > -30000 && window.Top > -30000, "屏外窗口恢复到显示器工作区");
        Check(window.SaveSettings(), "主窗口偏好持久化");
        // Leave a known persistent CF_UNICODETEXT payload for the parent process to inspect after exit.
        Check(await ClipboardService.CopyAsync("θðʃʒŋɜːaɪ\u0303"), "退出前写入持久化Unicode剪贴板");
        File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(new { success = true, time = DateTimeOffset.Now, checks, count = checks.Count, entries = all.Count, uniqueStrings = all.Select(x => x.Text).Distinct().Count(), groups = all.GroupBy(x => x.Page).Select(g => new { page = g.Key, count = g.Count() }) }, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    }
    private static async Task Settle() { await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle); await Task.Delay(60); }
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wp, IntPtr lp);
    private static async Task<string?> ReadClipboardAsync()
    {
        // Clipboard listeners may briefly acquire the clipboard immediately after a write.
        for (int i = 0; i < 30; i++)
        {
            var text = ClipboardService.ReadUnicode();
            if (text != null) return text;
            await Task.Delay(20);
        }
        return null;
    }
    private static void Render(MainWindow window, string path, double scale)
    {
        window.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * scale), (int)Math.Ceiling(window.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path); encoder.Save(file);
    }
}
