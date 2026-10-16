using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace IPAClipboard;

public sealed record SymbolEntry(string Id, string Text, string Display, string Name, string Example, string Page, string Group, bool Wrap, string Keywords)
{
    public string Tooltip => $"{Name}\n{Group}\n复制：{Text}  ({string.Join(" ", Text.EnumerateRunes().Select(r => $"U+{r.Value:X4}"))})\n右键收藏 · Enter / Space 复制";
    public bool Matches(string query) => string.IsNullOrWhiteSpace(query) ||
        $"{Text} {Display} {Name} {Example} {Group} {Keywords}".Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
}

public sealed class SymbolTile(SymbolEntry entry, bool favorite, bool mini) : INotifyPropertyChanged
{
    public SymbolEntry Entry { get; } = entry;
    public string Display => Entry.Display;
    public string Subtitle => string.IsNullOrEmpty(Entry.Example) ? Entry.Name : Entry.Example;
    public bool Favorite { get; } = favorite;
    public bool Mini { get; } = mini;
    private bool copied;
    public bool Copied { get => copied; set { copied = value; PropertyChanged?.Invoke(this, new(nameof(Copied))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
}
public sealed record SymbolGroup(string Title, IReadOnlyList<SymbolTile> Items);
public enum CopyFormat { Plain, Slashes, Brackets }

public sealed class UserSettings
{
    public double Width { get; set; } = 440;
    public double Height { get; set; } = 570;
    public double MiniWidth { get; set; } = 320;
    public double MiniHeight { get; set; } = 420;
    public double? Left { get; set; }
    public double? Top { get; set; }
    public bool Topmost { get; set; } = true;
    public bool Mini { get; set; }
    public string Page { get; set; } = "english";
    public string Category { get; set; } = "全部分类";
    public CopyFormat Format { get; set; }
    public List<string> Favorites { get; set; } = [];
    public List<string> Recent { get; set; } = [];
}
