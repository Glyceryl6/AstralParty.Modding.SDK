using System.Text;

namespace AstralParty.Modding.SDK;

/// <summary>
/// Self-contained, per-MOD JSON localization with content-based hot reload.
/// The caller supplies its own file locations, compiled defaults and optional
/// migration callbacks; the SDK has no knowledge of any MOD name or game UI.
/// A rejected JSON edit never discards the most recently valid translations.
/// </summary>
public sealed class JsonLocalizationStore {
    private readonly string _directory;
    private readonly string _languageConfig;
    private readonly string _defaultLocale;
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _defaults;
    private readonly LocalizationCatalog _catalog;
    private readonly Dictionary<string, ReloadableFile<Dictionary<string, string>>> _files =
        new(StringComparer.Ordinal);
    private float _nextPoll;

    public JsonLocalizationStore(string directory, string languageConfig,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> defaults,
        string defaultLocale = "zh_cn") {
        _directory = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory)));
        _languageConfig = Path.GetFullPath(languageConfig ?? throw new ArgumentNullException(nameof(languageConfig)));
        _defaultLocale = LocalizationCatalog.ValidateLocale(defaultLocale);
        _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
        if (!_defaults.TryGetValue(_defaultLocale, out var fallback))
            throw new ArgumentException("Default locale has no translations", nameof(defaults));
        _catalog = new LocalizationCatalog(fallback);
    }

    /// <summary>Optional host-specific migration of previously saved translations.</summary>
    public Action<string, IDictionary<string, string>>? BeforeMerge { get; set; }

    /// <summary>Called only after the merged JSON was successfully read and validated.</summary>
    public Action<string>? AfterRead { get; set; }

    public Action<string>? LogInfo { get; set; }
    public Action<string>? LogWarning { get; set; }
    public string CurrentLocale { get; private set; } = "";
    public string CurrentFile => CurrentLocale.Length == 0 ? "" : Path.Combine(_directory, CurrentLocale + ".json");
    public string Translate(string key) => _catalog.Translate(key);

    /// <summary>Poll on the host's main thread; returns true only when translations changed.</summary>
    public bool Poll(float now) {
        if (now < _nextPoll) return false;
        _nextPoll = now + 1f;
        try {
            Directory.CreateDirectory(_directory);
            if (!File.Exists(_languageConfig)) {
                Directory.CreateDirectory(Path.GetDirectoryName(_languageConfig)!);
                File.WriteAllText(_languageConfig,
                    "# Language=zh_cn | en_us | custom_locale\nLanguage=" + _defaultLocale + "\n",
                    new UTF8Encoding(false));
            }
            string locale = ReadLocale();
            IReadOnlyDictionary<string, string> fallback = _defaults.TryGetValue(locale, out var preferred)
                ? preferred : _defaults[_defaultLocale];
            string path = Path.Combine(_directory, locale + ".json");
            LocalizationCatalog.MergeDefaults(path, fallback, values => BeforeMerge?.Invoke(locale, values));
            if (!_files.TryGetValue(path, out var file)) {
                file = new ReloadableFile<Dictionary<string, string>>(path,
                    new Dictionary<string, string>(StringComparer.Ordinal), LocalizationCatalog.Parse);
                _files.Add(path, file);
            }
            bool changed = file.Reload() || !string.Equals(CurrentLocale, locale, StringComparison.Ordinal);
            if (changed) {
                _catalog.Use(file.Value);
                CurrentLocale = locale;
                LogInfo?.Invoke("Language loaded: " + locale + "; file=" + path);
            }
            // A caller may record a one-time migration only after a valid read,
            // without making the new SDK depend on that caller's old file layout.
            try { AfterRead?.Invoke(locale); }
            catch (Exception ex) { LogWarning?.Invoke("Language migration marker deferred: " + ex.Message); }
            return changed;
        } catch (Exception ex) {
            LogWarning?.Invoke("Language reload deferred (" + _directory + "): " + ex.Message);
            return false;
        }
    }

    private string ReadLocale() {
        string locale = _defaultLocale;
        foreach (string raw in File.ReadAllLines(_languageConfig)) {
            string line = raw.Trim();
            if (line.StartsWith("Language=", StringComparison.OrdinalIgnoreCase))
                locale = line.Substring(line.IndexOf('=') + 1);
        }
        return LocalizationCatalog.ValidateLocale(locale);
    }
}
