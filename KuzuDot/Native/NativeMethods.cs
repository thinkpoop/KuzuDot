using System;
using System.Runtime.InteropServices;
using KuzuDot.Native;
using KuzuDot.Native.Enums;

namespace KuzuDot.Native
{
    /// <summary>
    /// Wrapper for Native Kuzu methods via P/Invoke
    /// See libkuzu/kuzu.h for reference
    /// </summary>
    internal static class NativeMethods
    {
        private const string DllName = "kuzu_shared.dll";

        // Database functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_database_init([MarshalAs(UnmanagedType.LPStr)] string databasePath,
            KuzuSystemConfig systemConfig, out KuzuDatabase outDatabase);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_database_destroy(ref KuzuDatabase database);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuSystemConfig kuzu_default_system_config();

        // Connection functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_init(ref KuzuDatabase database, out KuzuConnection outConnection);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_connection_destroy(ref KuzuConnection connection);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_set_max_num_thread_for_exec(ref KuzuConnection connection, ulong numThreads);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_get_max_num_thread_for_exec(ref KuzuConnection connection, out ulong outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_connection_query(ref KuzuConnection connection,
            [MarshalAs(UnmanagedType.LPStr)] string query, out KuzuQueryResult outQueryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_connection_prepare(ref KuzuConnection connection,
            [MarshalAs(UnmanagedType.LPStr)] string query, out KuzuPreparedStatement outPreparedStatement);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_execute(ref KuzuConnection connection,
            ref KuzuPreparedStatement preparedStatement, out KuzuQueryResult outQueryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_connection_interrupt(ref KuzuConnection connection);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_connection_set_query_timeout(ref KuzuConnection connection, ulong timeoutInMs);

        // Prepared Statement functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_prepared_statement_destroy(ref KuzuPreparedStatement preparedStatement);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_prepared_statement_is_success(ref KuzuPreparedStatement preparedStatement);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_prepared_statement_get_error_message(ref KuzuPreparedStatement preparedStatement);

        // Prepared Statement bind functions - all data types
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_bool(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, bool value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_int64(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, long value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_int32(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, int value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_int16(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, short value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_int8(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, sbyte value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_uint64(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, ulong value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_uint32(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, uint value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_uint16(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, ushort value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_uint8(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, byte value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_double(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, double value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_float(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, float value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_date(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, KuzuDate value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, KuzuTimestamp value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp_ns(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, KuzuTimestampNs value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp_ms(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, KuzuTimestampMs value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp_sec(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, KuzuTimestampSec value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_timestamp_tz(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, KuzuTimestampTz value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_interval(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, KuzuInterval value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_string(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, [MarshalAs(UnmanagedType.LPStr)] string value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_prepared_statement_bind_value(ref KuzuPreparedStatement preparedStatement,
            [MarshalAs(UnmanagedType.LPStr)] string paramName, IntPtr value);

        // Query Result functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_query_result_destroy(ref KuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_query_result_is_success(ref KuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_query_result_get_error_message(ref KuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong kuzu_query_result_get_num_columns(ref KuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_column_name(ref KuzuQueryResult queryResult,
            ulong index, out IntPtr outColumnName);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_column_data_type(ref KuzuQueryResult queryResult,
            ulong index, out KuzuLogicalTypeNative outColumnDataType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong kuzu_query_result_get_num_tuples(ref KuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_query_result_has_next(ref KuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_next(ref KuzuQueryResult queryResult, out KuzuFlatTuple outFlatTuple);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_query_result_has_next_query_result(ref KuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_next_query_result(ref KuzuQueryResult queryResult,
            out KuzuQueryResult outNextQueryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_query_result_to_string(ref KuzuQueryResult queryResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_query_result_reset_iterator(ref KuzuQueryResult queryResult);

        // New: Query summary & Arrow interoperability
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_query_summary(ref KuzuQueryResult queryResult,
            out KuzuQuerySummary outQuerySummary);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_arrow_schema(ref KuzuQueryResult queryResult,
            out ArrowSchema outSchema);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_query_result_get_next_arrow_chunk(ref KuzuQueryResult queryResult,
            long chunkSize, out ArrowArray outArrowArray);

        // FlatTuple functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_flat_tuple_destroy(ref KuzuFlatTuple flatTuple);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_flat_tuple_get_value(ref KuzuFlatTuple flatTuple, ulong index, out KuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_flat_tuple_to_string(ref KuzuFlatTuple flatTuple);

        // DataType functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_data_type_create(KuzuDataTypeId id, ref KuzuLogicalTypeNative childType,
            ulong numElementsInArray, out KuzuLogicalTypeNative outType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_data_type_clone(ref KuzuLogicalTypeNative dataType, out KuzuLogicalTypeNative outType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_data_type_destroy(ref KuzuLogicalTypeNative dataType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_data_type_equals(ref KuzuLogicalTypeNative dataType1, ref KuzuLogicalTypeNative dataType2);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuDataTypeId kuzu_data_type_get_id(ref KuzuLogicalTypeNative dataType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_data_type_get_num_elements_in_array(ref KuzuLogicalTypeNative dataType, out ulong outResult);

        // Value creation functions - all data types
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_null();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_null_with_data_type(ref KuzuLogicalTypeNative dataType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_default(ref KuzuLogicalTypeNative dataType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_bool(bool value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int8(sbyte value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int16(short value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int32(int value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int64(long value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_uint8(byte value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_uint16(ushort value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_uint32(uint value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_uint64(ulong value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_int128(KuzuInt128 value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_float(float value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_double(double value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_internal_id(KuzuInternalIdNative value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_date(KuzuDate value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp(KuzuTimestamp value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp_ns(KuzuTimestampNs value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp_ms(KuzuTimestampMs value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp_sec(KuzuTimestampSec value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_timestamp_tz(KuzuTimestampTz value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_create_interval(KuzuInterval value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern IntPtr kuzu_value_create_string([MarshalAs(UnmanagedType.LPStr)] string value);

        // Collections / structured value creation
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_create_list(ulong numElements, IntPtr elements /* kuzu_value** */, out IntPtr outValue /* kuzu_value** */);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_create_struct(ulong numFields, IntPtr fieldNames /* const char** */, IntPtr fieldValues /* kuzu_value** */, out IntPtr outValue /* kuzu_value** */);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_create_map(ulong numFields, IntPtr keys /* kuzu_value** */, IntPtr values /* kuzu_value** */, out IntPtr outValue /* kuzu_value** */);

        // Value accessor functions - adding safety checks for IntPtr validation
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool kuzu_value_is_null(IntPtr value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_value_set_null(IntPtr value, bool isNull);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_value_get_data_type(IntPtr value, out KuzuLogicalTypeNative outType);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_bool(IntPtr value, out bool outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int8(IntPtr value, out sbyte outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int16(IntPtr value, out short outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int32(IntPtr value, out int outResult);

        // Fixed: Ensure proper marshaling for int64 to prevent memory access violations
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int64(IntPtr value, out long outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uint8(IntPtr value, out byte outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uint16(IntPtr value, out ushort outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uint32(IntPtr value, out uint outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uint64(IntPtr value, out ulong outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_int128(IntPtr value, out KuzuInt128 outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_float(IntPtr value, out float outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_double(IntPtr value, out double outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_internal_id(IntPtr value, out KuzuInternalIdNative outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_date(IntPtr value, out KuzuDate outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp(IntPtr value, out KuzuTimestamp outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp_ns(IntPtr value, out KuzuTimestampNs outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp_ms(IntPtr value, out KuzuTimestampMs outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp_sec(IntPtr value, out KuzuTimestampSec outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_timestamp_tz(IntPtr value, out KuzuTimestampTz outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_interval(IntPtr value, out KuzuInterval outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_string(IntPtr value, out IntPtr outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_blob(IntPtr value, out IntPtr outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_decimal_as_string(IntPtr value, out IntPtr outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_uuid(IntPtr value, out IntPtr outResult);

        // List/Array functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_list_size(IntPtr value, out ulong outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_list_element(IntPtr value, ulong index, out KuzuValue outValue);

        // Struct functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_struct_num_fields(IntPtr value, out ulong outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_struct_field_name(IntPtr value, ulong index, out IntPtr outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_struct_field_value(IntPtr value, ulong index, out KuzuValue outValue);

        // Map functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_map_size(IntPtr value, out ulong outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_map_key(IntPtr value, ulong index, out KuzuValue outKey);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_map_value(IntPtr value, ulong index, out KuzuValue outValue);

        // Recursive rel functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_recursive_rel_node_list(IntPtr value, out KuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_value_get_recursive_rel_rel_list(IntPtr value, out KuzuValue outValue);

        // Node value helpers
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_id_val(IntPtr nodeVal, out KuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_label_val(IntPtr nodeVal, out KuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_property_size(IntPtr nodeVal, out ulong outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_property_name_at(IntPtr nodeVal, ulong index, out IntPtr outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_get_property_value_at(IntPtr nodeVal, ulong index, out KuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_node_val_to_string(IntPtr nodeVal, out IntPtr outResult);

        // Rel value helpers
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_id_val(IntPtr relVal, out KuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_src_id_val(IntPtr relVal, out KuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_dst_id_val(IntPtr relVal, out KuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_label_val(IntPtr relVal, out KuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_property_size(IntPtr relVal, out ulong outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_property_name_at(IntPtr relVal, ulong index, out IntPtr outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_get_property_value_at(IntPtr relVal, ulong index, out KuzuValue outValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_rel_val_to_string(IntPtr relVal, out IntPtr outResult);

        // Value utility functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_clone(IntPtr value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_value_copy(IntPtr value, IntPtr other);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_value_destroy(IntPtr value);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_value_to_string(IntPtr value);

        // Int128 utility functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_int128_t_from_string([MarshalAs(UnmanagedType.LPStr)] string str, out KuzuInt128 outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_int128_t_to_string(KuzuInt128 val, out IntPtr outResult);

        // Date utility functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_date_to_string(KuzuDate date, out IntPtr outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern KuzuState kuzu_date_from_string([MarshalAs(UnmanagedType.LPStr)] string str, out KuzuDate outResult);

        // Timestamp/date <-> tm conversion
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_ns_to_tm(KuzuTimestampNs timestamp, out KuzuTm outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_ms_to_tm(KuzuTimestampMs timestamp, out KuzuTm outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_sec_to_tm(KuzuTimestampSec timestamp, out KuzuTm outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_tz_to_tm(KuzuTimestampTz timestamp, out KuzuTm outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_to_tm(KuzuTimestamp timestamp, out KuzuTm outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_ns_from_tm(KuzuTm tm, out KuzuTimestampNs outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_ms_from_tm(KuzuTm tm, out KuzuTimestampMs outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_sec_from_tm(KuzuTm tm, out KuzuTimestampSec outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_tz_from_tm(KuzuTm tm, out KuzuTimestampTz outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_timestamp_from_tm(KuzuTm tm, out KuzuTimestamp outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_date_to_tm(KuzuDate date, out KuzuTm outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern KuzuState kuzu_date_from_tm(KuzuTm tm, out KuzuDate outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_interval_to_difftime(KuzuInterval interval, out double outResult);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_interval_from_difftime(double diffTime, out KuzuInterval outResult);

        // QuerySummary functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_query_summary_destroy(ref KuzuQuerySummary querySummary);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern double kuzu_query_summary_get_compiling_time(ref KuzuQuerySummary querySummary);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern double kuzu_query_summary_get_execution_time(ref KuzuQuerySummary querySummary);

        // String and memory management
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_destroy_string(IntPtr str);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void kuzu_destroy_blob(IntPtr blob);

        // Version functions
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr kuzu_get_version();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern ulong kuzu_get_storage_version();
    }
}