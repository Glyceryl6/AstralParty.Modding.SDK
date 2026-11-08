using System;

namespace AstralParty.Modding.SDK.Native;

/// <summary>
/// Reusable, checked IL2CPP value-field access without a dependency on game
/// hot-update types or any one MOD's reflection cache. The host resolves
/// metadata and must call these helpers on its native runtime thread.
/// </summary>
public sealed class NativeTypedFields {
    private readonly Func<string, string, IntPtr> _field;

    /// <summary>注入字段查找函数；不同游戏版本应由宿主负责重新解析并校验元数据。</summary>
    public NativeTypedFields(Func<string, string, IntPtr> resolveField) {
        _field = resolveField ?? throw new ArgumentNullException(nameof(resolveField));
    }

    /// <summary>读取 1 字节的原生布尔字段；类型与长度由 Il2CppNative 校验。</summary>
    public bool ReadBool(IntPtr obj, string type, string name) =>
        Il2CppNative.ReadValue<byte>(obj, _field(type, name)) != 0;

    /// <summary>读取原生 Int32 字段；不存在或类型不匹配会抛出异常。</summary>
    public int ReadInt(IntPtr obj, string type, string name) =>
        Il2CppNative.ReadValue<int>(obj, _field(type, name));

    /// <summary>读取原生 float 字段；调用方负责保证对象在当前场景存活。</summary>
    public float ReadFloat(IntPtr obj, string type, string name) =>
        Il2CppNative.ReadValue<float>(obj, _field(type, name));

    /// <summary>写入 1 字节布尔字段；只应修改确知可写的游戏状态。</summary>
    public void WriteBool(IntPtr obj, string type, string name, bool value) =>
        Il2CppNative.WriteValue(obj, _field(type, name), value ? (byte)1 : (byte)0);

    /// <summary>写入经过类型检查的原生 Int32 字段。</summary>
    public void WriteInt(IntPtr obj, string type, string name, int value) =>
        Il2CppNative.WriteValue(obj, _field(type, name), value);

    /// <summary>写入经过类型检查的原生 float 字段。</summary>
    public void WriteFloat(IntPtr obj, string type, string name, float value) =>
        Il2CppNative.WriteValue(obj, _field(type, name), value);
}
