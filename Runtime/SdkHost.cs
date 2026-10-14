using System;
using System.Collections.Generic;

namespace AstralParty.Modding.SDK;

/// <summary>
/// Stable access point; the game's version-specific FairyGUI and HybridCLR
/// adapters are installed by AstralProbe, never by the consuming MOD.
/// SDK 只存托管契约，游戏调用交由宿主适配层处理。
/// </summary>
public static class SdkHost {
    public static Version ApiVersion { get; } = new(0, 6, 0);
    public static IRuntimeCapabilitySource? Capabilities { get; private set; }
    public static IModUiService? Ui { get; private set; }
    public static IClientValueRegistry? ClientValues { get; private set; }
    private static Func<IStatisticReader?>? _ownStatistics;
    private static Func<IIndexedStatisticReader?>? _indexedStatistics;

    /// <summary>Called once by AstralProbe's startup; not a game hook.</summary>
    public static void Initialize(IRuntimeCapabilitySource capabilities,
        Func<IStatisticReader?> ownStatistics) {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(ownStatistics);
        if (Capabilities != null) {
            throw new InvalidOperationException("AstralParty.Modding.SDK already initialized");
        }
        Capabilities = capabilities;
        _ownStatistics = ownStatistics;
    }

    /// <summary>Attach optional UI and reversible local-data services after Initialize.</summary>
    public static void AttachServices(IModUiService ui, IClientValueRegistry values) {
        ArgumentNullException.ThrowIfNull(ui);
        ArgumentNullException.ThrowIfNull(values);
        if (Capabilities == null || Ui != null || ClientValues != null) {
            throw new InvalidOperationException("AstralParty.Modding.SDK services cannot be attached twice or before Initialize");
        }
        Ui = ui;
        ClientValues = values;
    }

    /// <summary>Opt-in host provider for character/map keyed conditions.</summary>
    public static void AttachIndexedStatistics(Func<IIndexedStatisticReader?> readerFactory) {
        ArgumentNullException.ThrowIfNull(readerFactory);
        if (Capabilities == null || _indexedStatistics != null) {
            throw new InvalidOperationException("SDK indexed statistics provider was already attached or host is unavailable");
        }
        _indexedStatistics = readerFactory;
    }

    public static IReadOnlyList<IndexedStatisticRow> ReadOwnIndexedStatistics() {
        IIndexedStatisticReader? reader = _indexedStatistics?.Invoke();
        return reader == null ? Array.Empty<IndexedStatisticRow>() : IndexedStatistics.Present(reader);
    }

    /// <summary>Read only the currently signed-in account's server-supplied values.</summary>
    public static IReadOnlyList<StatisticValue> ReadOwnStatistics() {
        IStatisticReader? source = _ownStatistics?.Invoke();
        return source == null ? Array.Empty<StatisticValue>() : StatisticCatalog.Read(source);
    }
}
