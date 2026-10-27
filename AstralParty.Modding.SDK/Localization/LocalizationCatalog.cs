using System.Text;
using System.Text.Json;

namespace AstralParty.Modding.SDK;

/// <summary>One instance per MOD. The SDK contains no game text, paths or global locale.</summary>
public sealed class LocalizationCatalog {
    private readonly IReadOnlyDictionary<string, string> _fallback;
    private IReadOnlyDictionary<string, string> _current;

    public LocalizationCatalog(IReadOnlyDictionary<string, string> fallback) {
        _fallback = new Dictionary<string, string>(fallback, StringComparer.Ordinal);
        _current = _fallback;
    }

    public string Translate(string key) => _current.TryGetValue(key, out string? value) ? value :
        _fallback.TryGetValue(key, out value) ? value : key;

    public void Use(IReadOnlyDictionary<string, string> values) =>
        _current = new Dictionary<string, string>(values, StringComparer.Ordinal);

    public static Dictionary<string, string> Parse(string json) {
        using JsonDocument document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object) throw new FormatException("Language root must be an object");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (JsonProperty property in document.RootElement.EnumerateObject()) {
            if (!property.Name.Contains('.') || property.Value.ValueKind != JsonValueKind.String)
                throw new FormatException("Expected a namespaced translation key: " + property.Name);
            string value = property.Value.GetString() ?? "";
            if (value.Length == 0 || !result.TryAdd(property.Name, value))
                throw new FormatException("Empty or duplicate translation: " + property.Name);
        }
        if (result.Count == 0) throw new FormatException("Empty language file");
        return result;
    }

    public static void MergeDefaults(string path, IReadOnlyDictionary<string, string> defaults,
        Action<IDictionary<string, string>>? migrate = null) {
        bool exists = File.Exists(path);
        string? source = exists ? File.ReadAllText(path, Encoding.UTF8) : null;
        Dictionary<string, string> values = source != null ? Parse(source) : new(StringComparer.Ordinal);
        string original = JsonSerializer.Serialize(values);
        migrate?.Invoke(values);
        foreach (var pair in defaults) values.TryAdd(pair.Key, pair.Value);
        if (exists && JsonSerializer.Serialize(values) == original) return;
        if (exists && File.ReadAllText(path, Encoding.UTF8) != source)
            throw new IOException("Language file changed while merging: " + path);
        if (!exists && File.Exists(path)) throw new IOException("Language file was created while merging: " + path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            File.WriteAllText(temporary, JsonSerializer.Serialize(values, new JsonSerializerOptions {
                WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }) + "\n", new UTF8Encoding(false));
            File.Move(temporary, path, true);
        } finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static string ValidateLocale(string locale) {
        string value = locale.Trim().ToLowerInvariant();
        if (value.Length < 2 || value.Length > 20 || value.Any(c => !(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '_'))
            throw new FormatException("Invalid language identifier");
        return value;
    }
}
