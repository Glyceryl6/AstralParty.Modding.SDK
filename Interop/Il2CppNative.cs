using System.Runtime.InteropServices;

namespace AstralParty.Modding.SDK;

/// <summary>Optional IL2CPP primitives. Call only with live, correctly resolved metadata on the native UI thread.</summary>
public static unsafe class Il2CppNative {
    private const string Library = "GameAssembly";
    [DllImport(Library, EntryPoint = "il2cpp_gchandle_new", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint NewHandle(IntPtr obj, [MarshalAs(UnmanagedType.I1)] bool pinned);
    [DllImport(Library, EntryPoint = "il2cpp_gchandle_free", CallingConvention = CallingConvention.Cdecl)]
    private static extern void FreeHandle(uint handle);
    [DllImport(Library, EntryPoint = "il2cpp_string_length", CallingConvention = CallingConvention.Cdecl)]
    private static extern int StringLength(IntPtr value);
    [DllImport(Library, EntryPoint = "il2cpp_string_chars", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr StringChars(IntPtr value);
    [DllImport(Library, EntryPoint = "il2cpp_string_new", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr NewString([MarshalAs(UnmanagedType.LPUTF8Str)] string value);
    [DllImport(Library, EntryPoint = "il2cpp_object_get_class", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ObjectClass(IntPtr value);
    [DllImport(Library, EntryPoint = "il2cpp_class_get_name", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr ClassName(IntPtr type);
    [DllImport(Library, EntryPoint = "il2cpp_class_is_assignable_from", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool Assignable(IntPtr expected, IntPtr actual);
    [DllImport(Library, EntryPoint = "il2cpp_field_get_type", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FieldType(IntPtr field);
    [DllImport(Library, EntryPoint = "il2cpp_class_from_type", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr ClassFromType(IntPtr type);
    [DllImport(Library, EntryPoint = "il2cpp_type_get_type", CallingConvention = CallingConvention.Cdecl)]
    private static extern int TypeTag(IntPtr type);
    [DllImport(Library, EntryPoint = "il2cpp_class_is_valuetype", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool IsValueType(IntPtr type);
    [DllImport(Library, EntryPoint = "il2cpp_class_value_size", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ValueSize(IntPtr type, out uint alignment);
    [DllImport(Library, EntryPoint = "il2cpp_field_get_parent", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FieldParent(IntPtr field);
    [DllImport(Library, EntryPoint = "il2cpp_field_get_value", CallingConvention = CallingConvention.Cdecl)]
    private static extern void FieldGet(IntPtr obj, IntPtr field, void* value);
    [DllImport(Library, EntryPoint = "il2cpp_field_set_value", CallingConvention = CallingConvention.Cdecl)]
    private static extern void FieldSet(IntPtr obj, IntPtr field, void* value);
    [DllImport(Library, EntryPoint = "il2cpp_field_set_value_object", CallingConvention = CallingConvention.Cdecl)]
    private static extern void FieldSetObject(IntPtr obj, IntPtr field, IntPtr value);
    [DllImport(Library, EntryPoint = "il2cpp_runtime_invoke", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr RuntimeInvoke(IntPtr method, IntPtr obj, IntPtr* args, out IntPtr exception);
    [DllImport(Library, EntryPoint = "il2cpp_object_unbox", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr ObjectUnbox(IntPtr value);

    public static string ReadString(IntPtr value, int maximumLength = 1048576) {
        if (value == IntPtr.Zero) return "";
        using var lease = new ObjectLease(value);
        int length = StringLength(value);
        if (length < 0 || length > maximumLength) throw new InvalidOperationException("Invalid native string length");
        return length == 0 ? "" : Marshal.PtrToStringUni(StringChars(value), length) ?? "";
    }

    public static void RequireOwner(IntPtr obj, IntPtr expectedClass) {
        if (obj == IntPtr.Zero) throw new ArgumentNullException(nameof(obj));
        if (expectedClass == IntPtr.Zero || !Assignable(expectedClass, ObjectClass(obj)))
            throw new InvalidOperationException("Native owner type mismatch");
    }

    private static void RequireFieldValue<T>(IntPtr obj, IntPtr field) where T : unmanaged {
        if (field == IntPtr.Zero) throw new ArgumentNullException(nameof(field));
        RequireOwner(obj, FieldParent(field));
        IntPtr type = ClassFromType(FieldType(field));
        if (type == IntPtr.Zero || !IsValueType(type) || ValueSize(type, out _) != sizeof(T))
            throw new InvalidOperationException("Native value field size/type mismatch");
    }

    public static T ReadValue<T>(IntPtr obj, IntPtr field) where T : unmanaged {
        using var lease = new ObjectLease(obj);
        RequireFieldValue<T>(obj, field);
        T result = default;
        FieldGet(obj, field, &result);
        return result;
    }

    public static void WriteValue<T>(IntPtr obj, IntPtr field, T value) where T : unmanaged {
        using var lease = new ObjectLease(obj);
        RequireFieldValue<T>(obj, field);
        FieldSet(obj, field, &value);
    }

    public static void SetReference(IntPtr obj, IntPtr field, IntPtr expectedClass, IntPtr value) {
        RequireOwner(obj, expectedClass);
        if (field == IntPtr.Zero) throw new ArgumentNullException(nameof(field));
        RequireOwner(obj, FieldParent(field));
        using var ownerLease = new ObjectLease(obj);
        using var valueLease = new ObjectLease(value);
        IntPtr type = FieldType(field);
        int tag = TypeTag(type);
        if (tag != 0x0e && tag != 0x12 && tag != 0x14 && tag != 0x15 && tag != 0x1c && tag != 0x1d)
            throw new InvalidOperationException("Expected a native reference field");
        IntPtr klass = ClassFromType(type);
        if (klass == IntPtr.Zero || IsValueType(klass) || (value != IntPtr.Zero && !Assignable(klass, ObjectClass(value))))
            throw new InvalidOperationException("Native reference field type mismatch");
        FieldSetObject(obj, field, value);
        IntPtr actual;
        FieldGet(obj, field, &actual);
        if (actual != value) throw new InvalidOperationException("Native reference write did not round-trip");
    }

    /// <summary>args must use the IL2CPP ABI: references as object pointers, values as addresses of their native buffers.</summary>
    public static IntPtr InvokeInstance(IntPtr method, IntPtr obj, IntPtr expectedClass, IntPtr* args) {
        RequireOwner(obj, expectedClass);
        if (method == IntPtr.Zero) throw new ArgumentNullException(nameof(method));
        using var lease = new ObjectLease(obj);
        IntPtr result = RuntimeInvoke(method, obj, args, out IntPtr exception);
        if (exception != IntPtr.Zero)
            throw new InvalidOperationException("Native call raised " + (Marshal.PtrToStringUTF8(ClassName(ObjectClass(exception))) ?? "an exception"));
        return result;
    }

    public static T Unbox<T>(IntPtr boxed) where T : unmanaged {
        if (boxed == IntPtr.Zero) throw new ArgumentNullException(nameof(boxed));
        using var lease = new ObjectLease(boxed);
        IntPtr type = ObjectClass(boxed);
        if (!IsValueType(type) || ValueSize(type, out _) != sizeof(T)) throw new InvalidOperationException("Native boxed value size/type mismatch");
        T* pointer = (T*)ObjectUnbox(boxed).ToPointer();
        return *pointer;
    }

    public sealed class ObjectLease : IDisposable {
        private uint _handle;
        public ObjectLease(IntPtr value) { if (value != IntPtr.Zero) _handle = NewHandle(value, false); }
        public void Dispose() {
            uint handle = _handle;
            if (handle == 0) return;
            _handle = 0;
            FreeHandle(handle);
        }
    }
}
