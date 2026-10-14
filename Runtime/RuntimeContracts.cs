using System;
using System.Collections.Generic;

namespace AstralParty.Modding.SDK;

/// <summary>
/// Safe managed capability gate. Game objects and raw pointers must stay inside
/// the host adapter, never cross the public SDK boundary.
/// 功能探测契约：热更新升级后允许缺少成员并优雅降级，不缓存已销毁原生指针。
/// </summary>
public interface IRuntimeCapabilitySource {
    bool HasType(string fullName);
    bool HasMethod(string type, string method, int parameterCount);
    bool HasField(string type, string field);
}

public sealed record MemberRequirement(string Type, string Name, int Arity = -1);

public static class RuntimeCapabilities {
    public static bool Supports(IRuntimeCapabilitySource source,
        IReadOnlyList<MemberRequirement> requirements) {
        ArgumentNullException.ThrowIfNull(source);
        foreach (MemberRequirement item in requirements) {
            if (!source.HasType(item.Type)) {
                return false;
            }
            if (item.Arity < 0) {
                if (!source.HasField(item.Type, item.Name)) {
                    return false;
                }
            } else if (!source.HasMethod(item.Type, item.Name, item.Arity)) {
                return false;
            }
        }
        return true;
    }
}
