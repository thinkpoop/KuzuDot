using System;
using System.Runtime.InteropServices;
using KuzuDot.Native;
using KuzuDot.Native.Enums;
using KuzuDot.Utils;

namespace KuzuDot
{
    public partial class PreparedStatement : IDisposable
    {
        private sealed class PreparedStatementSafeHandle : SafeHandle
        {
            internal KuzuPreparedStatement NativeStruct;
            internal PreparedStatementSafeHandle(KuzuPreparedStatement nativeStruct) : base(IntPtr.Zero, true)
            {
                NativeStruct = nativeStruct; // keeps both PreparedStatement & BoundValues
                SetHandle(nativeStruct.PreparedStatement);
            }
            public override bool IsInvalid => handle == IntPtr.Zero;
            protected override bool ReleaseHandle()
            {
                try
                {
                    if (!IsInvalid)
                    {
                        NativeMethods.kuzu_prepared_statement_destroy(ref NativeStruct);
                        NativeStruct.PreparedStatement = IntPtr.Zero;
                        NativeStruct.BoundValues = IntPtr.Zero;
                        handle = IntPtr.Zero;
                    }
                    return true;
                }
                catch { return false; }
            }
        }

        private readonly PreparedStatementSafeHandle _handle;
        private readonly Connection _connection;

        internal PreparedStatement(KuzuPreparedStatement nativeHandle, Connection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _handle = new PreparedStatementSafeHandle(nativeHandle);
        }

        internal IntPtr NativePtr => _handle.DangerousGetHandle();
        internal ref KuzuPreparedStatement NativeStruct => ref _handle.NativeStruct;

        public bool IsSuccess
        {
            get
            {
                ThrowIfDisposed();
                return NativeMethods.kuzu_prepared_statement_is_success(ref _handle.NativeStruct);
            }
        }

        public string ErrorMessage
        {
            get
            {
                ThrowIfDisposed();
                return GetErrorMessageSafe();
            }
        }

        public override string ToString()
        {
            if (_handle.IsInvalid) return "PreparedStatement(Disposed)";
            return IsSuccess ? "PreparedStatement(Success)" : "PreparedStatement(Failed: " + ErrorMessage + ")";
        }

        // Primitive numeric/bool/string
        public void BindBool(string paramName, bool value) => Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_bool);
        public void BindInt8(string paramName, sbyte value) => Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_int8);
        public void BindInt16(string paramName, short value) => Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_int16);
        public void BindInt32(string paramName, int value) => Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_int32);
        public void BindInt64(string paramName, long value) => Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_int64);
        public void BindUInt8(string paramName, byte value) => Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_uint8);
        public void BindUInt16(string paramName, ushort value) => Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_uint16);
        public void BindUInt32(string paramName, uint value) => Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_uint32);
        public void BindUInt64(string paramName, ulong value) => Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_uint64);
        public void BindFloat(string paramName, float value) => Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_float);
        public void BindDouble(string paramName, double value) => Bind(paramName, value, NativeMethods.kuzu_prepared_statement_bind_double);
        public void BindString(string paramName, string value)
        {
            ThrowIfDisposed();
            KuzuGuard.NotNullOrEmpty(paramName, nameof(paramName));
            ValidateHandle();
            var result = NativeMethods.kuzu_prepared_statement_bind_string(ref _handle.NativeStruct, paramName, value ?? string.Empty);
            if (result != KuzuState.Success)
                throw new KuzuException($"Failed to bind string parameter '{paramName}': {GetErrorMessageSafe()}");
        }

        // Date
        public void BindDate(string paramName, DateTime value) => Bind(paramName, DateTimeUtilities.DateTimeToKuzuDate(value), NativeMethods.kuzu_prepared_statement_bind_date);

        // Timestamp (microsecond precision)
        public void BindTimestamp(string paramName, DateTime value)
            => Bind(paramName, DateTimeUtilities.DateTimeToNativeTimestamp(value), NativeMethods.kuzu_prepared_statement_bind_timestamp);
        public void BindTimestampMicros(string paramName, long unixMicros)
            => Bind(paramName, new KuzuTimestamp { Value = unixMicros }, NativeMethods.kuzu_prepared_statement_bind_timestamp);

        // Additional precisions (exposed as long based overloads for clarity)
        public void BindTimestampNanoseconds(string paramName, long unixNanos)
            => Bind(paramName, new KuzuTimestampNs { Value = unixNanos }, NativeMethods.kuzu_prepared_statement_bind_timestamp_ns);
        public void BindTimestampMilliseconds(string paramName, long unixMillis)
            => Bind(paramName, new KuzuTimestampMs { Value = unixMillis }, NativeMethods.kuzu_prepared_statement_bind_timestamp_ms);
        public void BindTimestampSeconds(string paramName, long unixSeconds)
            => Bind(paramName, new KuzuTimestampSec { Value = unixSeconds }, NativeMethods.kuzu_prepared_statement_bind_timestamp_sec);
        public void BindTimestampWithTimeZone(string paramName, DateTimeOffset dto)
            => Bind(paramName, new KuzuTimestampTz { Value = DateTimeUtilities.DateTimeToUnixMicroseconds(dto.UtcDateTime) }, NativeMethods.kuzu_prepared_statement_bind_timestamp_tz);

        // Interval
        public void BindInterval(string paramName, TimeSpan value)
            => Bind(paramName, DateTimeUtilities.TimeSpanToNativeInterval(value), NativeMethods.kuzu_prepared_statement_bind_interval);

        // Generic value
        public void BindValue(string paramName, KuzuValue value)
        {
            ThrowIfDisposed();
            KuzuGuard.NotNullOrEmpty(paramName, nameof(paramName));
            KuzuGuard.NotNull(value, nameof(value));
            ValidateHandle();
            var result = NativeMethods.kuzu_prepared_statement_bind_value(ref _handle.NativeStruct, paramName, value.Handle.Value);
            if (result != KuzuState.Success)
                throw new KuzuException($"Failed to bind value parameter '{paramName}': {GetErrorMessageSafe()}");
        }

        // Convenience generic overloads
        public void Bind(string p, bool v) => BindBool(p, v); public void Bind(string p, sbyte v) => BindInt8(p, v);
        public void Bind(string p, short v) => BindInt16(p, v); public void Bind(string p, int v) => BindInt32(p, v);
        public void Bind(string p, long v) => BindInt64(p, v); public void Bind(string p, byte v) => BindUInt8(p, v);
        public void Bind(string p, ushort v) => BindUInt16(p, v); public void Bind(string p, uint v) => BindUInt32(p, v);
        public void Bind(string p, ulong v) => BindUInt64(p, v); public void Bind(string p, float v) => BindFloat(p, v);
        public void Bind(string p, double v) => BindDouble(p, v); public void Bind(string p, string v) => BindString(p, v);
        public void Bind(string p, DateTime v) => BindTimestamp(p, v); public void Bind(string p, TimeSpan v) => BindInterval(p, v);
        public void Bind(string p, KuzuValue v) => BindValue(p, v);

        public QueryResult Execute()
        {
            ThrowIfDisposed();
            return _connection.Execute(this);
        }

        public void Dispose()
        {
            _handle.Dispose();
            GC.SuppressFinalize(this);
        }

        private delegate KuzuState NativeBind<T>(ref KuzuPreparedStatement handle, string paramName, T value);
        private void Bind<T>(string paramName, T value, NativeBind<T> binder)
        {
            ThrowIfDisposed();
            KuzuGuard.NotNullOrEmpty(paramName, nameof(paramName));
            ValidateHandle();
            var result = binder(ref _handle.NativeStruct, paramName, value);
            if (result != KuzuState.Success)
                throw new KuzuException($"Failed to bind parameter '{paramName}': {GetErrorMessageSafe()}");
        }

        private bool IsValidHandle() => !_handle.IsInvalid;
        private void ValidateHandle() { if (!IsValidHandle()) throw new InvalidOperationException("Invalid PreparedStatement handle"); }
        private string GetErrorMessageSafe()
        {
            if (!IsValidHandle()) return "Invalid prepared statement handle";
            var ptr = NativeMethods.kuzu_prepared_statement_get_error_message(ref _handle.NativeStruct);
            return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }
        private void ThrowIfDisposed() { if (_handle.IsInvalid) throw new ObjectDisposedException(nameof(PreparedStatement)); }
    }
}