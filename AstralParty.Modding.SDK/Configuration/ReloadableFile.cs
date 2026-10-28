using System.Text;

namespace AstralParty.Modding.SDK;

/// <summary>Poll a file and accept a new snapshot only after the caller validates it.</summary>
public sealed class ReloadableFile<T> {
    private readonly string _path;
    private readonly Func<string, T> _parse;
    private string? _lastContent;
    private bool _loaded;

    public ReloadableFile(string path, T initial, Func<string, T> parse) {
        _path = Path.GetFullPath(path);
        _parse = parse ?? throw new ArgumentNullException(nameof(parse));
        Value = initial;
    }

    public T Value { get; private set; }

    public bool Reload(bool force = false) {
        var before = new FileInfo(_path);
        // Timestamp + length alone misses edits performed within a filesystem's
        // clock resolution (especially replacing a translation with equally
        // long text). Compare actual content on each low-frequency poll.
        string text = File.ReadAllText(_path, Encoding.UTF8);
        var after = new FileInfo(_path);
        if (before.LastWriteTimeUtc != after.LastWriteTimeUtc || before.Length != after.Length)
            throw new IOException("Configuration changed while reading: " + _path);
        if (!force && _loaded && string.Equals(text, _lastContent, StringComparison.Ordinal))
            return false;
        T parsed = _parse(text);
        Value = parsed;
        _lastContent = text;
        _loaded = true;
        return true;
    }
}
