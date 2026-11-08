using System;

namespace AstralParty.Modding.SDK;

/// <summary>
/// Reusable, checked IL2CPP value-field access without a dependency on game
/// hot-update types or any one MOD's reflection cache. The host resolves
/// metadata and must call these helpers on its native runtime thread.
/// </summary>
public sealed class NativeTypedFields {
    private readonly Func<string, string, IntPtr> _field;

    public NativeTypedFields(Func<string, string, IntPtr> resolveField) {
        _field = resolveField ?? throw new ArgumentNullException(nameof(resolveField));
    }

    public bool ReadBool(IntPtr obj, string type, string name) =>
        Il2CppNative.ReadValue<byte>(obj, _field(type, name)) != 0;

    public int ReadInt(IntPtr obj, string type, string name) =>
        Il2CppNative.ReadValue<int>(obj, _field(type, name));

    public float ReadFloat(IntPtr obj, string type, string name) =>
        Il2CppNative.ReadValue<float>(obj, _field(type, name));

    public void WriteBool(IntPtr obj, string type, string name, bool value) =>
        Il2CppNative.WriteValue(obj, _field(type, name), value ? (byte)1 : (byte)0);

    public void WriteInt(IntPtr obj, string type, string name, int value) =>
        Il2CppNative.WriteValue(obj, _field(type, name), value);

    public void WriteFloat(IntPtr obj, string type, string name, float value) =>
        Il2CppNative.WriteValue(obj, _field(type, name), value);
}
