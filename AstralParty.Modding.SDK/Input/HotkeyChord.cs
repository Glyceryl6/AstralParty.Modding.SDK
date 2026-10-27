namespace AstralParty.Modding.SDK;

/// <summary>Engine-independent chord syntax. Hosts decide supported and reserved keys.</summary>
public readonly record struct HotkeyChord(string Key, bool Control = false, bool Shift = false, bool Alt = false) {
    public bool IsNone => string.IsNullOrEmpty(Key) || Key.Equals("None", StringComparison.OrdinalIgnoreCase);
    public override string ToString() => IsNone ? "None" : (Control ? "Ctrl+" : "") +
        (Shift ? "Shift+" : "") + (Alt ? "Alt+" : "") + Key;

    public static bool TryParse(string value, Func<string, string?> normalizeKey, out HotkeyChord chord) {
        chord = new("None");
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (value.Trim().Equals("None", StringComparison.OrdinalIgnoreCase)) return true;
        bool ctrl = false, shift = false, alt = false;
        string? key = null;
        foreach (string token in value.Split('+')) {
            string part = token.Trim();
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase)) {
                if (ctrl) return false;
                ctrl = true;
            } else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) {
                if (shift) return false;
                shift = true;
            } else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) {
                if (alt) return false;
                alt = true;
            } else {
                if (key != null || (key = normalizeKey(part)) == null) return false;
            }
        }
        if (key == null) return false;
        chord = new(key, ctrl, shift, alt);
        return true;
    }
}

/// <summary>An extensible binding list; registration does not invoke an input API.</summary>
public sealed class HotkeyRegistry {
    public sealed record Binding(string Id, string Label, Func<string> Read, Action<string> Write);
    private readonly List<Binding> _bindings = new();
    public int Revision { get; private set; }
    public IReadOnlyList<Binding> Bindings => _bindings.AsReadOnly();

    public IDisposable Register(string id, string label, Func<string> read, Action<string> write) {
        if (string.IsNullOrWhiteSpace(id) || _bindings.Any(b => b.Id == id)) throw new ArgumentException("Duplicate/empty binding id", nameof(id));
        var binding = new Binding(id, label, read ?? throw new ArgumentNullException(nameof(read)), write ?? throw new ArgumentNullException(nameof(write)));
        _bindings.Add(binding);
        Revision++;
        return new Registration(() => { if (_bindings.Remove(binding)) Revision++; });
    }

    public Binding? FindConflict(string exceptId, string value, Func<string, string?> normalize) {
        if (!HotkeyChord.TryParse(value, normalize, out var requested) || requested.IsNone) return null;
        return _bindings.FirstOrDefault(b => b.Id != exceptId &&
            HotkeyChord.TryParse(b.Read(), normalize, out var existing) && existing == requested);
    }

    private sealed class Registration : IDisposable {
        private Action? _release;
        internal Registration(Action release) { _release = release; }
        public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
    }
}
