using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Numerics;
using KuzuDot.Native;
using KuzuDot.Native.Enums;

namespace KuzuDot
{
    public class KuzuValue : IDisposable
    {
        private sealed class KuzuValueSafeHandle : SafeHandle
        {
            internal bool IsOwnedByCppNative;
            internal bool AllocatedWrapper;
            internal KuzuValueSafeHandle() : base(IntPtr.Zero, true) { }
            internal KuzuValueSafeHandle(IntPtr ptr, bool isOwnedByCppNative, bool allocatedWrapper) : base(IntPtr.Zero, true)
            { IsOwnedByCppNative = isOwnedByCppNative; AllocatedWrapper = allocatedWrapper; SetHandle(ptr); }
            public override bool IsInvalid => handle == IntPtr.Zero;
            protected override bool ReleaseHandle()
            { try { if (!IsInvalid) { if (AllocatedWrapper) Marshal.FreeHGlobal(handle); else if (!IsOwnedByCppNative) NativeMethods.kuzu_value_destroy(handle); handle = IntPtr.Zero; } return true; } catch { return false; } }
        }

        private readonly KuzuValueSafeHandle _handle;
        private readonly object _lockObject = new object();
        private KuzuValue(KuzuValueSafeHandle handle) { _handle = handle; }

        public static KuzuValue CreateNull() => CreateOwned(NativeMethods.kuzu_value_create_null(), "null");
        public static KuzuValue CreateBool(bool value) => CreateOwned(NativeMethods.kuzu_value_create_bool(value), "boolean");
        public static KuzuValue CreateInt8(sbyte value) => CreateOwned(NativeMethods.kuzu_value_create_int8(value), "int8");
        public static KuzuValue CreateInt16(short value) => CreateOwned(NativeMethods.kuzu_value_create_int16(value), "int16");
        public static KuzuValue CreateInt32(int value) => CreateOwned(NativeMethods.kuzu_value_create_int32(value), "int32");
        public static KuzuValue CreateInt64(long value) => CreateOwned(NativeMethods.kuzu_value_create_int64(value), "int64");
        public static KuzuValue CreateUInt8(byte value) => CreateOwned(NativeMethods.kuzu_value_create_uint8(value), "uint8");
        public static KuzuValue CreateUInt16(ushort value) => CreateOwned(NativeMethods.kuzu_value_create_uint16(value), "uint16");
        public static KuzuValue CreateUInt32(uint value) => CreateOwned(NativeMethods.kuzu_value_create_uint32(value), "uint32");
        public static KuzuValue CreateUInt64(ulong value) => CreateOwned(NativeMethods.kuzu_value_create_uint64(value), "uint64");
        internal static KuzuValue CreateInt128Internal(KuzuInt128 value) => CreateOwned(NativeMethods.kuzu_value_create_int128(value), "int128");
        public static KuzuValue CreateFloat(float value) => CreateOwned(NativeMethods.kuzu_value_create_float(value), "float");
        public static KuzuValue CreateDouble(double value) => CreateOwned(NativeMethods.kuzu_value_create_double(value), "double");
        public static KuzuValue CreateInternalId(InternalId value) => CreateOwned(NativeMethods.kuzu_value_create_internal_id(value.ToNative()), "internal ID");
        internal static KuzuValue CreateDateInternal(KuzuDate value) => CreateOwned(NativeMethods.kuzu_value_create_date(value), "date");
        public static KuzuValue CreateDate(DateTime dateTime) => CreateDateInternal(DateTimeUtilities.DateTimeToKuzuDate(dateTime));
        public static KuzuValue CreateTimestamp(DateTime dateTime) => CreateOwned(NativeMethods.kuzu_value_create_timestamp(DateTimeUtilities.DateTimeToNativeTimestamp(dateTime)), "timestamp");
        public static KuzuValue CreateTimestampFromUnixMicros(long micros) => CreateOwned(NativeMethods.kuzu_value_create_timestamp(new KuzuTimestamp { Value = micros }), "timestamp");
        // Additional timestamp precision creation helpers
        public static KuzuValue CreateTimestampNanoseconds(long nanos) => CreateOwned(NativeMethods.kuzu_value_create_timestamp_ns(new KuzuTimestampNs { Value = nanos }), "timestamp_ns");
        public static KuzuValue CreateTimestampMilliseconds(long millis) => CreateOwned(NativeMethods.kuzu_value_create_timestamp_ms(new KuzuTimestampMs { Value = millis }), "timestamp_ms");
        public static KuzuValue CreateTimestampSeconds(long seconds) => CreateOwned(NativeMethods.kuzu_value_create_timestamp_sec(new KuzuTimestampSec { Value = seconds }), "timestamp_sec");
        public static KuzuValue CreateTimestampWithTimeZoneMicros(long microsUtc) => CreateOwned(NativeMethods.kuzu_value_create_timestamp_tz(new KuzuTimestampTz { Value = microsUtc }), "timestamp_tz");
        public static KuzuValue CreateInterval(TimeSpan span) => CreateOwned(NativeMethods.kuzu_value_create_interval(DateTimeUtilities.TimeSpanToNativeInterval(span)), "interval");
        public static KuzuValue CreateString(string value) { if (value == null) throw new ArgumentNullException(nameof(value)); return CreateOwned(NativeMethods.kuzu_value_create_string(value), "string"); }
        public static KuzuValue CreateBigInteger(BigInteger value) { var native = BigIntegerToNative(value); return CreateInt128Internal(native); }

        // Collection / structured creation helpers
        public static KuzuValue CreateList(params KuzuValue[] elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            IntPtr outValPtr;
            IntPtr elemsPtr = IntPtr.Zero;
            try
            {
                if (elements.Length > 0)
                {
                    int size = IntPtr.Size * elements.Length;
                    elemsPtr = Marshal.AllocHGlobal(size);
                    for (int i = 0; i < elements.Length; i++)
                    {
                        if (elements[i] == null) throw new ArgumentNullException($"elements[{i}]");
                        var ptr = elements[i]._handle.DangerousGetHandle();
                        Marshal.WriteIntPtr(elemsPtr, i * IntPtr.Size, ptr);
                    }
                }
                var state = NativeMethods.kuzu_value_create_list((ulong)elements.Length, elemsPtr, out outValPtr);
                if (state != KuzuState.Success || outValPtr == IntPtr.Zero)
                    throw new KuzuException("Failed to create list value");
            }
            finally { if (elemsPtr != IntPtr.Zero) Marshal.FreeHGlobal(elemsPtr); }
            return CreateOwned(outValPtr, "list");
        }

        public static KuzuValue CreateStruct(params (string Name, KuzuValue Value)[] fields)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            IntPtr outValPtr; IntPtr namesPtr = IntPtr.Zero; IntPtr valuesPtr = IntPtr.Zero; var allocatedNames = new IntPtr[fields.Length];
            try
            {
                if (fields.Length > 0)
                {
                    namesPtr = Marshal.AllocHGlobal(IntPtr.Size * fields.Length);
                    valuesPtr = Marshal.AllocHGlobal(IntPtr.Size * fields.Length);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        if (string.IsNullOrEmpty(fields[i].Name)) throw new ArgumentException("Field name cannot be null or empty", nameof(fields));
                        if (fields[i].Value == null) throw new ArgumentNullException($"fields[{i}].Value");
                        var namePtr = Marshal.StringToHGlobalAnsi(fields[i].Name);
                        allocatedNames[i] = namePtr;
                        Marshal.WriteIntPtr(namesPtr, i * IntPtr.Size, namePtr);
                        Marshal.WriteIntPtr(valuesPtr, i * IntPtr.Size, fields[i].Value._handle.DangerousGetHandle());
                    }
                }
                var state = NativeMethods.kuzu_value_create_struct((ulong)fields.Length, namesPtr, valuesPtr, out outValPtr);
                if (state != KuzuState.Success || outValPtr == IntPtr.Zero)
                    throw new KuzuException("Failed to create struct value");
            }
            finally
            {
                for (int i = 0; i < allocatedNames.Length; i++) if (allocatedNames[i] != IntPtr.Zero) Marshal.FreeHGlobal(allocatedNames[i]);
                if (namesPtr != IntPtr.Zero) Marshal.FreeHGlobal(namesPtr);
                if (valuesPtr != IntPtr.Zero) Marshal.FreeHGlobal(valuesPtr);
            }
            return CreateOwned(outValPtr, "struct");
        }

        public static KuzuValue CreateMap(KuzuValue[] keys, KuzuValue[] values)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (keys.Length != values.Length) throw new ArgumentException("Keys and values length mismatch");
            IntPtr outValPtr; IntPtr keysPtr = IntPtr.Zero; IntPtr valuesPtr = IntPtr.Zero;
            try
            {
                if (keys.Length > 0)
                {
                    keysPtr = Marshal.AllocHGlobal(IntPtr.Size * keys.Length);
                    valuesPtr = Marshal.AllocHGlobal(IntPtr.Size * values.Length);
                    for (int i = 0; i < keys.Length; i++)
                    {
                        if (keys[i] == null) throw new ArgumentNullException($"keys[{i}]");
                        if (values[i] == null) throw new ArgumentNullException($"values[{i}]");
                        Marshal.WriteIntPtr(keysPtr, i * IntPtr.Size, keys[i]._handle.DangerousGetHandle());
                        Marshal.WriteIntPtr(valuesPtr, i * IntPtr.Size, values[i]._handle.DangerousGetHandle());
                    }
                }
                var state = NativeMethods.kuzu_value_create_map((ulong)keys.Length, keysPtr, valuesPtr, out outValPtr);
                if (state != KuzuState.Success || outValPtr == IntPtr.Zero)
                    throw new KuzuException("Failed to create map value");
            }
            finally
            {
                if (keysPtr != IntPtr.Zero) Marshal.FreeHGlobal(keysPtr);
                if (valuesPtr != IntPtr.Zero) Marshal.FreeHGlobal(valuesPtr);
            }
            return CreateOwned(outValPtr, "map");
        }

        // NOTE: There is no native create-blob API; users must provide blobs via query literals/casts.

        private static KuzuInt128 BigIntegerToNative(BigInteger value)
        { var bytes = value.ToByteArray(); if (bytes.Length > 16) throw new OverflowException("BigInteger does not fit into 128 bits"); byte[] padded = new byte[16]; byte fill = (value.Sign < 0) ? (byte)0xFF : (byte)0x00; for (int i = 0; i < 16; i++) padded[i] = fill; Array.Copy(bytes, 0, padded, 0, bytes.Length); ulong low = BitConverter.ToUInt64(padded, 0); long high = BitConverter.ToInt64(padded, 8); return new KuzuInt128 { Low = low, High = high }; }
        private static BigInteger NativeToBigInteger(KuzuInt128 native) { byte[] bytes = new byte[16]; Array.Copy(BitConverter.GetBytes(native.Low), 0, bytes, 0, 8); Array.Copy(BitConverter.GetBytes(native.High), 0, bytes, 8, 8); return new BigInteger(bytes); }
        private static KuzuValue CreateOwned(IntPtr ptr, string kind) { if (ptr == IntPtr.Zero) throw new KuzuException($"Failed to create {kind} value"); return new KuzuValue(new KuzuValueSafeHandle(ptr, false, false)); }

        public static KuzuValue CreateDateFromString(string dateString) { if (string.IsNullOrEmpty(dateString)) throw new ArgumentException("Date string cannot be null or empty", nameof(dateString)); var state = NativeMethods.kuzu_date_from_string(dateString, out var d); if (state != KuzuState.Success) throw new KuzuException($"Failed to parse date from string: {dateString}"); return CreateDateInternal(d); }
        public static KuzuValue CreateBigIntegerFromString(string int128String) { if (string.IsNullOrEmpty(int128String)) throw new ArgumentException("Integer string cannot be null or empty", nameof(int128String)); var state = NativeMethods.kuzu_int128_t_from_string(int128String, out var v); if (state != KuzuState.Success) throw new KuzuException($"Failed to parse int128 from string: {int128String}"); return CreateInt128Internal(v); }

        internal KuzuDot.Native.KuzuValue Handle => new KuzuDot.Native.KuzuValue { Value = _handle.DangerousGetHandle(), IsOwnedByCpp = _handle.IsOwnedByCppNative };

        public bool IsNull() { lock (_lockObject) { ThrowIfDisposed(); if (_handle.IsInvalid) return true; return NativeMethods.kuzu_value_is_null(_handle.DangerousGetHandle()); } }
        public void SetNull(bool isNull) { lock (_lockObject) { EnsureAliveAndValid(); NativeMethods.kuzu_value_set_null(_handle.DangerousGetHandle(), isNull); } }

        public DataType GetDataType() { lock (_lockObject) { EnsureAliveAndValid(); NativeMethods.kuzu_value_get_data_type(_handle.DangerousGetHandle(), out KuzuLogicalTypeNative t); return DataType.FromBorrowed(in t); } }

        public bool GetBool() => GetPrimitive("boolean", NativeMethods.kuzu_value_get_bool, out bool v) ? v : default;
        public sbyte GetInt8() => GetPrimitive("int8", NativeMethods.kuzu_value_get_int8, out sbyte v) ? v : default;
        public short GetInt16() => GetPrimitive("int16", NativeMethods.kuzu_value_get_int16, out short v) ? v : default;
        public int GetInt32() => GetPrimitive("int32", NativeMethods.kuzu_value_get_int32, out int v) ? v : default;
        public long GetInt64() { lock (_lockObject) { EnsureAliveAndValid(); var state = NativeMethods.kuzu_value_get_int64(_handle.DangerousGetHandle(), out var val); if (state != KuzuState.Success) throw new KuzuException("Failed to get int64 value - type mismatch or invalid value"); return val; } }
        public byte GetUInt8() => GetPrimitive("uint8", NativeMethods.kuzu_value_get_uint8, out byte v) ? v : default;
        public ushort GetUInt16() => GetPrimitive("uint16", NativeMethods.kuzu_value_get_uint16, out ushort v) ? v : default;
        public uint GetUInt32() => GetPrimitive("uint32", NativeMethods.kuzu_value_get_uint32, out uint v) ? v : default;
        public ulong GetUInt64() => GetPrimitive("uint64", NativeMethods.kuzu_value_get_uint64, out ulong v) ? v : default;
        internal KuzuInt128 GetNativeInt128() => GetPrimitive("int128", NativeMethods.kuzu_value_get_int128, out KuzuInt128 v) ? v : default;
        public BigInteger GetBigInteger() => NativeToBigInteger(GetNativeInt128());
        public float GetFloat() => GetPrimitive("float", NativeMethods.kuzu_value_get_float, out float v) ? v : default;
        public double GetDouble() => GetPrimitive("double", NativeMethods.kuzu_value_get_double, out double v) ? v : default;
        public InternalId GetInternalId() { var native = GetPrimitive("internal id", NativeMethods.kuzu_value_get_internal_id, out KuzuInternalIdNative v) ? v : default; return new InternalId(native); }
        private KuzuDate GetNativeDate() { lock (_lockObject) { EnsureAliveAndValid(); var state = NativeMethods.kuzu_value_get_date(_handle.DangerousGetHandle(), out KuzuDate v); if (state != KuzuState.Success) throw new KuzuException("Failed to get date value - type mismatch or invalid value"); return v; } }
        public DateTime GetDate() => DateTimeUtilities.KuzuDateToDateTime(GetNativeDate());
        public DateTime GetTimestampAsDateTime() { lock (_lockObject) { EnsureAliveAndValid(); var state = NativeMethods.kuzu_value_get_timestamp(_handle.DangerousGetHandle(), out KuzuTimestamp ts); if (state != KuzuState.Success) throw new KuzuException("Failed to get timestamp value"); return DateTimeUtilities.NativeTimestampToDateTime(ts); } }
        public long GetTimestampUnixMicros() { lock (_lockObject) { EnsureAliveAndValid(); var state = NativeMethods.kuzu_value_get_timestamp(_handle.DangerousGetHandle(), out KuzuTimestamp ts); if (state != KuzuState.Success) throw new KuzuException("Failed to get timestamp value"); return ts.Value; } }
        public long GetTimestampNsUnixNanoseconds() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_value_get_timestamp_ns(_handle.DangerousGetHandle(), out KuzuTimestampNs ts); if (st != KuzuState.Success) throw new KuzuException("Failed to get timestamp_ns value"); return ts.Value; } }
        public long GetTimestampMsUnixMilliseconds() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_value_get_timestamp_ms(_handle.DangerousGetHandle(), out KuzuTimestampMs ts); if (st != KuzuState.Success) throw new KuzuException("Failed to get timestamp_ms value"); return ts.Value; } }
        public long GetTimestampSecUnixSeconds() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_value_get_timestamp_sec(_handle.DangerousGetHandle(), out KuzuTimestampSec ts); if (st != KuzuState.Success) throw new KuzuException("Failed to get timestamp_sec value"); return ts.Value; } }
        public long GetTimestampTzUnixMicros() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_value_get_timestamp_tz(_handle.DangerousGetHandle(), out KuzuTimestampTz ts); if (st != KuzuState.Success) throw new KuzuException("Failed to get timestamp_tz value"); return ts.Value; } }
        public TimeSpan GetInterval() { lock (_lockObject) { EnsureAliveAndValid(); var state = NativeMethods.kuzu_value_get_interval(_handle.DangerousGetHandle(), out KuzuInterval iv); if (state != KuzuState.Success) throw new KuzuException("Failed to get interval value"); return DateTimeUtilities.NativeIntervalToTimeSpan(iv); } }
        public string GetString() { lock (_lockObject) { EnsureAliveAndValid(); var state = NativeMethods.kuzu_value_get_string(_handle.DangerousGetHandle(), out var ptr); if (state != KuzuState.Success) throw new KuzuException("Failed to get string value - type mismatch or invalid value"); if (ptr == IntPtr.Zero) return string.Empty; try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; } finally { NativeMethods.kuzu_destroy_string(ptr); } } }
        public string GetDecimalAsString() { lock (_lockObject) { EnsureAliveAndValid(); var state = NativeMethods.kuzu_value_get_decimal_as_string(_handle.DangerousGetHandle(), out var ptr); if (state != KuzuState.Success) throw new KuzuException("Failed to get decimal value"); if (ptr == IntPtr.Zero) return string.Empty; try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; } finally { NativeMethods.kuzu_destroy_string(ptr); } } }
        public string GetUuid() { lock (_lockObject) { EnsureAliveAndValid(); var state = NativeMethods.kuzu_value_get_uuid(_handle.DangerousGetHandle(), out var ptr); if (state != KuzuState.Success) throw new KuzuException("Failed to get uuid value"); if (ptr == IntPtr.Zero) return string.Empty; try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; } finally { NativeMethods.kuzu_destroy_string(ptr); } } }
        public byte[] GetBlob()
        {
            lock (_lockObject)
            {
                EnsureAliveAndValid();
                var state = NativeMethods.kuzu_value_get_blob(_handle.DangerousGetHandle(), out var ptr);
                if (state != KuzuState.Success) throw new KuzuException("Failed to get blob value - type mismatch or invalid value");
                if (ptr == IntPtr.Zero) return new byte[0];
                try
                {
                    var hex = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
                    if (hex.Length == 0) return Array.Empty<byte>();
                    hex = hex.Trim();
                    if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex.Substring(2);
                    if (hex.Length % 2 != 0) throw new KuzuException("Invalid blob hex length returned from native layer");
                    int len = hex.Length / 2;
                    var bytes = new byte[len];
                    for (int i = 0; i < len; i++) bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                    return bytes;
                }
                finally { NativeMethods.kuzu_destroy_blob(ptr); }
            }
        }
        public ulong GetListSize() => GetPrimitive("list size", NativeMethods.kuzu_value_get_list_size, out ulong s) ? s : 0UL;
        public KuzuValue GetListElement(ulong index) { lock (_lockObject) { EnsureAliveAndValid(); var state = NativeMethods.kuzu_value_get_list_element(_handle.DangerousGetHandle(), index, out var h); if (state != KuzuState.Success) throw new KuzuException($"Failed to get list element at index {index}"); h.IsOwnedByCpp = true; return CreateBorrowedFromRaw(h); } }
        public ulong GetStructNumFields() => GetPrimitive("struct field count", NativeMethods.kuzu_value_get_struct_num_fields, out ulong c) ? c : 0UL;
        public string GetStructFieldName(ulong index) { lock (_lockObject) { EnsureAliveAndValid(); var state = NativeMethods.kuzu_value_get_struct_field_name(_handle.DangerousGetHandle(), index, out var ptr); if (state != KuzuState.Success) throw new KuzuException($"Failed to get struct field name at index {index}"); if (ptr == IntPtr.Zero) return string.Empty; try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; } finally { NativeMethods.kuzu_destroy_string(ptr); } } }
        public KuzuValue GetStructFieldValue(ulong index) { lock (_lockObject) { EnsureAliveAndValid(); var state = NativeMethods.kuzu_value_get_struct_field_value(_handle.DangerousGetHandle(), index, out var h); if (state != KuzuState.Success) throw new KuzuException($"Failed to get struct field value at index {index}"); h.IsOwnedByCpp = true; return CreateBorrowedFromRaw(h); } }

        // Map helpers
        public ulong GetMapSize() => GetPrimitive("map size", NativeMethods.kuzu_value_get_map_size, out ulong s) ? s : 0UL;
        public KuzuValue GetMapKey(ulong index) { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_value_get_map_key(_handle.DangerousGetHandle(), index, out var h); if (st != KuzuState.Success) throw new KuzuException($"Failed to get map key at index {index}"); h.IsOwnedByCpp = true; return CreateBorrowedFromRaw(h); } }
        public KuzuValue GetMapValue(ulong index) { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_value_get_map_value(_handle.DangerousGetHandle(), index, out var h); if (st != KuzuState.Success) throw new KuzuException($"Failed to get map value at index {index}"); h.IsOwnedByCpp = true; return CreateBorrowedFromRaw(h); } }

        // Recursive rel helpers
        public KuzuValue GetRecursiveRelNodeList() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_value_get_recursive_rel_node_list(_handle.DangerousGetHandle(), out var h); if (st != KuzuState.Success) throw new KuzuException("Failed to get recursive rel node list"); h.IsOwnedByCpp = true; return CreateBorrowedFromRaw(h); } }
        public KuzuValue GetRecursiveRelRelList() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_value_get_recursive_rel_rel_list(_handle.DangerousGetHandle(), out var h); if (st != KuzuState.Success) throw new KuzuException("Failed to get recursive rel rel list"); h.IsOwnedByCpp = true; return CreateBorrowedFromRaw(h); } }

        // Node helpers
        public KuzuValue GetNodeIdValue() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_node_val_get_id_val(_handle.DangerousGetHandle(), out var h); if (st != KuzuState.Success) throw new KuzuException("Failed to get node id value"); h.IsOwnedByCpp = true; return CreateBorrowedFromRaw(h); } }
        public KuzuValue GetNodeLabelValue() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_node_val_get_label_val(_handle.DangerousGetHandle(), out var h); if (st != KuzuState.Success) throw new KuzuException("Failed to get node label value"); h.IsOwnedByCpp = true; return CreateBorrowedFromRaw(h); } }
        public ulong GetNodePropertySize() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_node_val_get_property_size(_handle.DangerousGetHandle(), out ulong sz); if (st != KuzuState.Success) throw new KuzuException("Failed to get node property size"); return sz; } }
        public string GetNodePropertyNameAt(ulong index) { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_node_val_get_property_name_at(_handle.DangerousGetHandle(), index, out var ptr); if (st != KuzuState.Success) throw new KuzuException($"Failed to get node property name at index {index}"); if (ptr == IntPtr.Zero) return string.Empty; try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; } finally { NativeMethods.kuzu_destroy_string(ptr); } } }
        public KuzuValue GetNodePropertyValueAt(ulong index) { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_node_val_get_property_value_at(_handle.DangerousGetHandle(), index, out var h); if (st != KuzuState.Success) throw new KuzuException($"Failed to get node property value at index {index}"); h.IsOwnedByCpp = true; return CreateBorrowedFromRaw(h); } }
        public string GetNodeString() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_node_val_to_string(_handle.DangerousGetHandle(), out var ptr); if (st != KuzuState.Success) throw new KuzuException("Failed to convert node to string"); if (ptr == IntPtr.Zero) return string.Empty; try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; } finally { NativeMethods.kuzu_destroy_string(ptr); } } }

        // Rel helpers
        public KuzuValue GetRelIdValue() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_rel_val_get_id_val(_handle.DangerousGetHandle(), out var h); if (st != KuzuState.Success) throw new KuzuException("Failed to get rel id value"); h.IsOwnedByCpp = true; return CreateBorrowedFromRaw(h); } }
        public KuzuValue GetRelSrcIdValue() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_rel_val_get_src_id_val(_handle.DangerousGetHandle(), out var h); if (st != KuzuState.Success) throw new KuzuException("Failed to get rel src id value"); h.IsOwnedByCpp = true; return CreateBorrowedFromRaw(h); } }
        public KuzuValue GetRelDstIdValue() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_rel_val_get_dst_id_val(_handle.DangerousGetHandle(), out var h); if (st != KuzuState.Success) throw new KuzuException("Failed to get rel dst id value"); h.IsOwnedByCpp = true; return CreateBorrowedFromRaw(h); } }
        public KuzuValue GetRelLabelValue() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_rel_val_get_label_val(_handle.DangerousGetHandle(), out var h); if (st != KuzuState.Success) throw new KuzuException("Failed to get rel label value"); h.IsOwnedByCpp = true; return CreateBorrowedFromRaw(h); } }
        public ulong GetRelPropertySize() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_rel_val_get_property_size(_handle.DangerousGetHandle(), out ulong sz); if (st != KuzuState.Success) throw new KuzuException("Failed to get rel property size"); return sz; } }
        public string GetRelPropertyNameAt(ulong index) { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_rel_val_get_property_name_at(_handle.DangerousGetHandle(), index, out var ptr); if (st != KuzuState.Success) throw new KuzuException($"Failed to get rel property name at index {index}"); if (ptr == IntPtr.Zero) return string.Empty; try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; } finally { NativeMethods.kuzu_destroy_string(ptr); } } }
        public KuzuValue GetRelPropertyValueAt(ulong index) { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_rel_val_get_property_value_at(_handle.DangerousGetHandle(), index, out var h); if (st != KuzuState.Success) throw new KuzuException($"Failed to get rel property value at index {index}"); h.IsOwnedByCpp = true; return CreateBorrowedFromRaw(h); } }
        public string GetRelString() { lock (_lockObject) { EnsureAliveAndValid(); var st = NativeMethods.kuzu_rel_val_to_string(_handle.DangerousGetHandle(), out var ptr); if (st != KuzuState.Success) throw new KuzuException("Failed to convert rel to string"); if (ptr == IntPtr.Zero) return string.Empty; try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; } finally { NativeMethods.kuzu_destroy_string(ptr); } } }

        public KuzuValue Clone() { lock (_lockObject) { EnsureAliveAndValid(); var clone = NativeMethods.kuzu_value_clone(_handle.DangerousGetHandle()); if (clone == IntPtr.Zero) throw new KuzuException("Failed to clone value"); return new KuzuValue(new KuzuValueSafeHandle(clone, false, false)); } }
        public void CopyFrom(KuzuValue other) { lock (_lockObject) { EnsureAliveAndValid(); if (other == null) throw new ArgumentNullException(nameof(other)); other.ThrowIfDisposed(); NativeMethods.kuzu_value_copy(_handle.DangerousGetHandle(), other._handle.DangerousGetHandle()); } }
        public string GetDateAsString() => StructToString(GetNativeDate(), NativeMethods.kuzu_date_to_string, "date");
        public string GetBigIntegerAsString() => StructToString(GetNativeInt128(), NativeMethods.kuzu_int128_t_to_string, "int128");
        public string GetInternalIdAsString() => GetInternalId().ToString();
        public override string ToString() { lock (_lockObject) { if (_handle.IsInvalid) return "[Invalid KuzuValue]"; var ptr = NativeMethods.kuzu_value_to_string(_handle.DangerousGetHandle()); if (ptr == IntPtr.Zero) return string.Empty; try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; } finally { NativeMethods.kuzu_destroy_string(ptr); } } }
        public void Dispose() { _handle.Dispose(); GC.SuppressFinalize(this); }

        private delegate KuzuState NativeGetter<T>(IntPtr value, out T outVal);
        private delegate KuzuState StructToStringConverter<TStruct>(TStruct val, out IntPtr strPtr);
        private bool GetPrimitive<T>(string name, NativeGetter<T> getter, out T value) { lock (_lockObject) { EnsureAliveAndValid(); var state = getter(_handle.DangerousGetHandle(), out value); if (state != KuzuState.Success) throw new KuzuException($"Failed to get {name} value - type mismatch or invalid value"); return true; } }
        private string StructToString<TStruct>(TStruct val, StructToStringConverter<TStruct> converter, string name) { var state = converter(val, out var ptr); if (state != KuzuState.Success) throw new KuzuException($"Failed to convert {name} to string"); if (ptr == IntPtr.Zero) return string.Empty; try { return Marshal.PtrToStringAnsi(ptr) ?? string.Empty; } finally { NativeMethods.kuzu_destroy_string(ptr); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] private void ThrowIfDisposed() { if (_handle.IsInvalid) throw new ObjectDisposedException(nameof(KuzuValue)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] private void ValidateHandle() { if (_handle.IsInvalid) throw new InvalidOperationException("Invalid KuzuValue handle - pointer is null"); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] private void EnsureAliveAndValid() { ThrowIfDisposed(); ValidateHandle(); }
        internal static KuzuValue CreateBorrowedFromRaw(KuzuDot.Native.KuzuValue raw) { int size = Marshal.SizeOf(typeof(KuzuDot.Native.KuzuValue)); var wrapperPtr = Marshal.AllocHGlobal(size); Marshal.StructureToPtr(raw, wrapperPtr, false); return new KuzuValue(new KuzuValueSafeHandle(wrapperPtr, true, true)); }
    }
}