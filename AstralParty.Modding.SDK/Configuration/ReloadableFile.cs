using System.Text;

namespace AstralParty.Modding.SDK;

/// <summary>Poll a file and accept a new snapshot only after the caller validates it.</summary>
public sealed class ReloadableFile<T> {
    private readonly string _path;
    private readonly Func<string, T> _parse;
    private DateTime _stamp = DateTime.MinValue;
    private long _length = -1;
    private bool _loaded;

    public ReloadableFile(string path, T initial, Func<string, T> parse) {
        _path = Path.GetFullPath(path);
        _parse = parse ?? throw new ArgumentNullException(nameof(parse));
        Value = initial;
    }

    public T Value { get; private set; }

    public bool Reload(bool force = false) {
        var before = new FileInfo(_path);
        if (!force && _loaded && before.LastWriteTimeUtc == _stamp && before.Length == _length) return false;
        string text = File.ReadAllText(_path, Encoding.UTF8);
        T parsed = _parse(text);
        var after = new FileInfo(_path);
        if (before.LastWriteTimeUtc != after.LastWriteTimeUtc || before.Length != after.Length)
            throw new IOException("Configuration changed while reading: " + _path);
        Value = parsed;
        _stamp = after.LastWriteTimeUtc;
        _length = after.Length;
        _loaded = true;
        return true;
    }
}
