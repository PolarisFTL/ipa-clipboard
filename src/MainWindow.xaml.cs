using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace IPAClipboard;

public partial class MainWindow : Window
{
    internal readonly UserSettings Settings;
    private readonly SettingsStore store;
    private readonly UsageHistory history;
    private bool ready;
    private readonly DispatcherTimer feedback = new() { Interval = TimeSpan.FromMilliseconds(1100) };
    private readonly DispatcherTimer saveTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly SemaphoreSlim copyGate = new(1, 1);
    private int copyVersion;
    private SymbolTile? highlighted;
    internal IReadOnlyList<SymbolGroup> VisibleGroups { get; private set; } = [];

    public MainWindow(SettingsStore? settingsStore = null)
    {
        store = settingsStore ?? new(); Settings = store.Load(); history = new(Settings);
        InitializeComponent();
        MinHeight = Settings.Mini ? 300 : 360;
        Topmost = Settings.Topmost;
        Width = Settings.Mini ? Settings.MiniWidth : Settings.Width;
        Height = Settings.Mini ? Settings.MiniHeight : Settings.Height;
        if (Settings.Left is { } left && Settings.Top is { } top)
        { WindowStartupLocation = WindowStartupLocation.Manual; Left = left; Top = top; }
        FormatBox.SelectedIndex = (int)Settings.Format;
        ready = true;
        SelectTab(); UpdateChrome(); RefreshCategories();
        feedback.Tick += (_, _) => { feedback.Stop(); if (highlighted != null) highlighted.Copied = false; StatusLabel.Text = "单击复制 · 右键收藏"; StatusLabel.Foreground = (Brush)FindResource("Muted"); };
        saveTimer.Tick += (_, _) => { saveTimer.Stop(); SaveSettings(); };
        LocationChanged += (_, _) => QueueSave(); SizeChanged += (_, _) => QueueSave();
        Loaded += (_, _) => { EnsureOnScreen(); QueueSave(); };
        SourceInitialized += (_, _) => HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(WindowMessages);
        Closing += OnClosing;
    }
    private IntPtr WindowMessages(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp, ref bool handled)
    { if (msg is 0x007E or 0x02E0 or 0x0232) Dispatcher.BeginInvoke(EnsureOnScreen, DispatcherPriority.Background); return IntPtr.Zero; }
    internal void EnsureOnScreen()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var monitor = MonitorFromWindow(hwnd, 2);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) return;
        var matrix = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var a = matrix.Transform(new Point(info.Work.Left, info.Work.Top)); var b = matrix.Transform(new Point(info.Work.Right, info.Work.Bottom));
        var width = Math.Max(1, b.X - a.X); var height = Math.Max(1, b.Y - a.Y);
        if (Width > width) Width = Math.Max(MinWidth, width);
        if (Height > height) Height = Math.Max(MinHeight, height);
        Left = Math.Clamp(double.IsFinite(Left) ? Left : a.X, a.X, Math.Max(a.X, b.X - ActualWidth));
        Top = Math.Clamp(double.IsFinite(Top) ? Top : a.Y, a.Y, Math.Max(a.Y, b.Y - ActualHeight));
    }
    private void SelectTab()
    {
        (Settings.Page switch { "ipa" => IpaTab, "common" => CommonTab, "saved" => SavedTab, _ => EnglishTab }).IsChecked = true;
    }
    private void UpdateChrome()
    {
        PinButton.ToolTip = Topmost ? "取消置顶" : "始终置顶";
        PinButton.Background = Topmost ? new SolidColorBrush(Color.FromRgb(219, 234, 254)) : Brushes.Transparent;
        ((TextBlock)PinButton.Content).Foreground = (Brush)FindResource(Topmost ? "Accent" : "Muted");
        MiniButton.ToolTip = Settings.Mini ? "恢复完整模式" : "迷你模式";
        TitleLabel.FontSize = Settings.Mini ? 12 : 14;
        FormatLabel.Visibility = Settings.Mini ? Visibility.Collapsed : Visibility.Visible;
        PageNote.Visibility = Settings.Mini ? Visibility.Collapsed : Visibility.Visible;
    }
    private void RefreshCategories()
    {
        if (!ready) return;
        var desired = Settings.Category;
        ready = false;
        CategoryBox.ItemsSource = new[] { "全部分类" }.Concat(Settings.Page == "saved" ? new[] { "常用收藏", "最近使用" } : Catalog.All.Where(x => x.Page == Settings.Page).Select(x => x.Group).Distinct()).ToList();
        CategoryBox.SelectedItem = ((IEnumerable<string>)CategoryBox.ItemsSource).Contains(desired) ? desired : "全部分类";
        Settings.Category = CategoryBox.SelectedItem?.ToString() ?? "全部分类";
        ready = true; RefreshSymbols();
    }
    internal void RefreshSymbols()
    {
        if (!ready) return;
        string query = SearchBox.Text;
        SearchHint.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Collapsed;
        var groups = new List<SymbolGroup>();
        SymbolTile Tile(SymbolEntry e) => new(e, Settings.Favorites.Contains(e.Id), Settings.Mini);
        bool Included(string title) => Settings.Category == "全部分类" || Settings.Category == title;
        if (Settings.Page == "saved")
        {
            foreach (var (title, ids) in new[] { ("常用收藏", Settings.Favorites), ("最近使用", Settings.Recent) })
            {
                var items = ids.Select(id => Catalog.All.FirstOrDefault(x => x.Id == id)).OfType<SymbolEntry>().Where(x => x.Matches(query)).Select(Tile).ToList();
                if (Included(title) && items.Count > 0) groups.Add(new($"{title} · {items.Count}", items));
            }
        }
        else
        {
            groups.AddRange(Catalog.All.Where(x => x.Page == Settings.Page && Included(x.Group) && x.Matches(query)).GroupBy(x => x.Group).Select(g => new SymbolGroup($"{g.Key} · {g.Count()}", g.Select(Tile).ToList())));
        }
        VisibleGroups = groups; GroupsList.ItemsSource = groups;
        int count = groups.Sum(g => g.Items.Count); CountLabel.Text = $"{count} 项";
        EmptyLabel.Text = Settings.Page == "saved" && string.IsNullOrWhiteSpace(query) ? "右键音标即可收藏。\n复制过的音标会出现在最近使用中。" : "没有匹配的音标，试试其他关键词。";
        EmptyLabel.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        PageNote.Text = Settings.Page switch
        {
            "english" => "教材 48 = 常用 44 + 4 个教学组合。ts / dz / tr / dr 通常不作为独立音位；示例以英式读音为主。",
            "ipa" => "按国际语音协会图表分类 · 悬停查看名称与码位。◌ 仅作附着位置示意，复制时自动去除。",
            "common" => "重音与附加符号按原字符复制，不添加括号。◌ 不会被复制；附加符号请粘贴到基字之后。",
            _ => "最近使用保留 12 项 · 右键可添加或取消收藏。"
        };
    }
    private void PageChanged(object sender, RoutedEventArgs e)
    { if (!ready) return; Settings.Page = (string)((RadioButton)sender).Tag; Settings.Category = "全部分类"; RefreshCategories(); SymbolScroll.ScrollToTop(); QueueSave(); }
    private void CategoryChanged(object sender, SelectionChangedEventArgs e)
    { if (!ready) return; Settings.Category = CategoryBox.SelectedItem?.ToString() ?? "全部分类"; RefreshSymbols(); SymbolScroll.ScrollToTop(); QueueSave(); }
    private void FormatChanged(object sender, SelectionChangedEventArgs e)
    { if (!ready) return; Settings.Format = (CopyFormat)Math.Max(0, FormatBox.SelectedIndex); QueueSave(); }
    private void SearchChanged(object sender, TextChangedEventArgs e) { if (ready) { RefreshSymbols(); SymbolScroll.ScrollToTop(); } }
    private void ClearSearch(object sender, RoutedEventArgs e) { SearchBox.Clear(); SearchBox.Focus(); }
    private async void CopySymbol(object sender, RoutedEventArgs e) { if (((Button)sender).DataContext is SymbolTile tile) await CopyEntryAsync(tile); }
    private async void SymbolKeyDown(object sender, KeyEventArgs e)
    { if (e.Key == Key.Enter && ((Button)sender).DataContext is SymbolTile tile) { e.Handled = true; await CopyEntryAsync(tile); } }
    internal async Task<bool> CopyEntryAsync(SymbolTile tile)
    {
        int request = ++copyVersion; var text = Catalog.Format(tile.Entry, Settings.Format);
        await copyGate.WaitAsync();
        try
        {
            if (request != copyVersion) return false;
            bool success = await ClipboardService.CopyAsync(text);
            if (request != copyVersion) return success;
            if (success)
            {
                history.Record(tile.Entry); QueueSave();
                if (highlighted != null) highlighted.Copied = false;
                highlighted = tile; tile.Copied = true;
                ShowStatus($"✓ 已复制 {tile.Entry.Display}");
                // Defer recent-list rebuilding until the next view change to keep keyboard focus stable.
            }
            else ShowStatus("剪贴板正忙，未复制，请再次单击。", true);
            return success;
        }
        finally { copyGate.Release(); }
    }
    public void ShowStatus(string text, bool error = false)
    { StatusLabel.Text = text; StatusLabel.Foreground = new SolidColorBrush(error ? Color.FromRgb(180, 83, 9) : Color.FromRgb(37, 99, 235)); feedback.Stop(); feedback.Interval = TimeSpan.FromMilliseconds(error ? 2200 : 1100); feedback.Start(); }
    private void FavoriteMenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu menu && menu.PlacementTarget is Button { DataContext: SymbolTile tile })
            ((MenuItem)menu.Items[0]).Header = Settings.Favorites.Contains(tile.Entry.Id) ? "★ 取消收藏" : "☆ 收藏音标";
    }
    private void FavoriteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Parent: ContextMenu menu } && menu.PlacementTarget is Button { DataContext: SymbolTile tile })
        { history.ToggleFavorite(tile.Entry); RefreshSymbols(); QueueSave(); }
    }
    internal void SwitchMini()
    {
        RememberSize(); Settings.Mini = !Settings.Mini;
        MinHeight = Settings.Mini ? 300 : 360;
        Width = Settings.Mini ? Settings.MiniWidth : Settings.Width; Height = Settings.Mini ? Settings.MiniHeight : Settings.Height;
        UpdateChrome(); RefreshSymbols(); Dispatcher.BeginInvoke(EnsureOnScreen, DispatcherPriority.Loaded); QueueSave();
    }
    private void ToggleMini(object sender, RoutedEventArgs e) => SwitchMini();
    private void TogglePin(object sender, RoutedEventArgs e) { Topmost = !Topmost; Settings.Topmost = Topmost; UpdateChrome(); QueueSave(); }
    private void MinimizeWindow(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void CloseWindow(object sender, RoutedEventArgs e) => Close();
    private void RememberSize()
    {
        if (WindowState != WindowState.Normal) return;
        if (Settings.Mini) { Settings.MiniWidth = Width; Settings.MiniHeight = Height; }
        else { Settings.Width = Width; Settings.Height = Height; }
    }
    private void QueueSave() { if (!ready) return; saveTimer.Stop(); saveTimer.Start(); }
    internal bool SaveSettings()
    { RememberSize(); if (WindowState == WindowState.Normal) { Settings.Left = Left; Settings.Top = Top; } Settings.Topmost = Topmost; return store.Save(Settings); }
    private void OnClosing(object? sender, CancelEventArgs e) { saveTimer.Stop(); feedback.Stop(); SaveSettings(); }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
}
