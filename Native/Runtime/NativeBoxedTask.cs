#nullable enable
using System;
using System.Runtime.InteropServices;
using IL2CppSharp;


namespace AstralParty.Modding.SDK.Native {
    /// <summary>Consume a native UniTask through its reference-type source, never a boxed struct method.</summary>
    public sealed class NativeBoxedTask : IDisposable {
        private readonly IntPtr _task, _source, _getStatus, _getResult, _syncResult;
        private readonly NativeObjectLease _taskLease;
        private readonly NativeObjectLease? _sourceLease;
        private readonly short _token;
        private readonly bool _expectsBoolean;
        private bool _consumed;
        public bool Consumed => _consumed;

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_field_get_value")]
        private static extern void ReadObject(IntPtr obj, IntPtr field, out IntPtr value);
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_field_get_value")]
        private static extern void ReadInt16(IntPtr obj, IntPtr field, out short value);
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_field_get_value")]
        private static extern void ReadByte(IntPtr obj, IntPtr field, out byte value);
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_field_get_type")]
        private static extern IntPtr FieldType(IntPtr field);
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_from_type")]
        private static extern IntPtr ClassFromType(IntPtr type);
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_is_assignable_from")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool Assignable(IntPtr expected, IntPtr actual);
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_is_valuetype")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool IsValueType(IntPtr klass);
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_object_get_virtual_method")]
        private static extern IntPtr VirtualMethod(IntPtr obj, IntPtr method);
        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_method_get_return_type")]
        private static extern IntPtr ReturnType(IntPtr method);

        public NativeBoxedTask(IntPtr task, bool expectsBoolean = true) {
            if (task == IntPtr.Zero) throw new ArgumentNullException(nameof(task));
            _task = task;
            _expectsBoolean = expectsBoolean;
            _taskLease = new NativeObjectLease(task);
            try {
                IntPtr klass = Il2CppReflection.GetObjectClass(task);
                if (!IsValueType(klass)) throw new InvalidOperationException("Expected a boxed native UniTask value");
                IntPtr sourceField = RequireField(klass, "source");
                IntPtr tokenField = RequireField(klass, "token");
                IntPtr sourceInterface = ClassFromType(FieldType(sourceField));
                IntPtr contract = FairyBridge.Class("Cysharp.Threading.Tasks.IUniTaskSource");
                if (sourceInterface == IntPtr.Zero || !Assignable(contract, sourceInterface) ||
                    ClassFromType(FieldType(tokenField)) != FairyBridge.Class("System.Int16"))
                    throw new InvalidOperationException("Unsupported native UniTask source/token field signatures");
                ReadObject(task, sourceField, out _source);
                ReadInt16(task, tokenField, out _token);
                if (_source == IntPtr.Zero) {
                    if (_expectsBoolean) {
                        _syncResult = RequireField(klass, "result");
                        if (ClassFromType(FieldType(_syncResult)) != FairyBridge.Class("System.Boolean"))
                            throw new InvalidOperationException("Expected a Boolean native UniTask result");
                    }
                    return;
                }
                IntPtr actual = Il2CppReflection.GetObjectClass(_source);
                if (IsValueType(actual) || !Assignable(sourceInterface, actual))
                    throw new InvalidOperationException("Native UniTask source is not a compatible reference type");
                _sourceLease = new NativeObjectLease(_source);
                IntPtr status = Il2CppReflection.FindMethod(contract, "GetStatus", new[] { FairyBridge.Class("System.Int16") });
                IntPtr result = Il2CppReflection.FindMethod(_expectsBoolean ? sourceInterface : contract,
                    "GetResult", new[] { FairyBridge.Class("System.Int16") });
                if (status == IntPtr.Zero || result == IntPtr.Zero)
                    throw new MissingMethodException("Native IUniTaskSource.GetStatus/GetResult(Int16)");
                if (_expectsBoolean && ClassFromType(ReturnType(result)) != FairyBridge.Class("System.Boolean"))
                    throw new InvalidOperationException("Native task result method is not Boolean");
                _getStatus = VirtualMethod(_source, status);
                _getResult = VirtualMethod(_source, result);
                if (_getStatus == IntPtr.Zero || _getResult == IntPtr.Zero)
                    throw new MissingMethodException("Native UniTask interface implementation");
            } catch {
                _sourceLease?.Dispose();
                _taskLease.Dispose();
                throw;
            }
        }

        public bool TryComplete(out bool succeeded) {
            succeeded = false;
            if (_consumed) return true;
            if (_source == IntPtr.Zero) {
                _consumed = true;
                if (_expectsBoolean) {
                    ReadByte(_task, _syncResult, out byte result);
                    succeeded = result != 0;
                } else succeeded = true;
                return true;
            }
            // The token is a two-byte Int16, not an Int32 or an object pointer.
            IntPtr token = Il2CppReflection.BoxValue(FairyBridge.Class("System.Int16"), _token);
            using var tokenLease = new NativeObjectLease(token);
            int status = Il2CppReflection.InvokeGet<int>(_getStatus, _source, new[] { token });
            if (status == 0) return false;
            if (status < 1 || status > 3) throw new InvalidOperationException("Invalid native UniTask status: " + status);
            // GetResult consumes the source exactly once, including a fault or
            // cancellation. Never resubmit a consumed pooled-source token.
            _consumed = true;
            IntPtr boxed = Il2CppReflection.InvokeMethod(_getResult, _source, new[] { token });
            using var resultLease = new NativeObjectLease(boxed);
            succeeded = status == 1 && (!_expectsBoolean ||
                (boxed != IntPtr.Zero && Il2CppReflection.UnboxValue<bool>(boxed)));
            return true;
        }

        private static IntPtr RequireField(IntPtr klass, string name) {
            IntPtr field = Il2CppReflection.FindField(klass, name);
            if (field == IntPtr.Zero) throw new MissingFieldException("Native UniTask." + name);
            return field;
        }

        public void Dispose() {
            _sourceLease?.Dispose();
            _taskLease.Dispose();
        }
    }
}
