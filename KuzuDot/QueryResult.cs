using System;
using System.Runtime.InteropServices;
using KuzuDot.Native;
using KuzuDot.Native.Enums;
using KuzuDot.Utils;

namespace KuzuDot
{
    public class QueryResult : IDisposable
    {
        private sealed class QueryResultSafeHandle : SafeHandle
        {
            internal bool IsOwnedByCpp;
            internal QueryResultSafeHandle() : base(IntPtr.Zero, true) { }
            public override bool IsInvalid => handle == IntPtr.Zero;
            internal void Initialize(IntPtr ptr) { SetHandle(ptr); }
            protected override bool ReleaseHandle()
            {
                try
                {
                    if (!IsInvalid && !IsOwnedByCpp)
                    {
                        var native = new KuzuQueryResult { QueryResult = handle };
                        NativeMethods.kuzu_query_result_destroy(ref native);
                    }
                    handle = IntPtr.Zero;
                    return true;
                }
                catch { return false; }
            }
        }

        private readonly QueryResultSafeHandle _handle = new QueryResultSafeHandle();

        internal QueryResult(KuzuQueryResult nativeHandle)
        {
            _handle.IsOwnedByCpp = nativeHandle.IsOwnedByCpp;
            _handle.Initialize(nativeHandle.QueryResult);
            if (!IsSuccess)
            {
                string errorMessage = "Unknown error";
                try
                {
                    var copy = new KuzuQueryResult { QueryResult = nativeHandle.QueryResult };
                    var errorPtr = NativeMethods.kuzu_query_result_get_error_message(ref copy);
                    errorMessage = NativeUtil.PtrToStringAndDestroy(errorPtr, NativeMethods.kuzu_destroy_string);
                }
                catch (Exception ex)
                {
                    errorMessage = $"Query failed with native error (unable to retrieve details: {ex.Message})";
                }
                throw new KuzuException($"Query failed: {errorMessage}");
            }
        }

        private KuzuQueryResult AsStruct() => new KuzuQueryResult { QueryResult = _handle.DangerousGetHandle(), IsOwnedByCpp = _handle.IsOwnedByCpp };
        private void ThrowIfDisposed() { if (_handle.IsInvalid) throw new ObjectDisposedException(nameof(QueryResult)); }

        public bool IsSuccess
        {
            get
            {
                ThrowIfDisposed();
                var s = AsStruct();
                return NativeMethods.kuzu_query_result_is_success(ref s);
            }
        }

        public string GetErrorMessage()
        {
            ThrowIfDisposed();
            var s = AsStruct();
            var errorPtr = NativeMethods.kuzu_query_result_get_error_message(ref s);
            return NativeUtil.PtrToStringAndDestroy(errorPtr, NativeMethods.kuzu_destroy_string);
        }

        public ulong NumColumns
        {
            get
            {
                ThrowIfDisposed();
                var s = AsStruct();
                return NativeMethods.kuzu_query_result_get_num_columns(ref s);
            }
        }

        public ulong GetNumColumns() => NumColumns;

        public ulong GetNumTuples()
        {
            ThrowIfDisposed();
            var s = AsStruct();
            return NativeMethods.kuzu_query_result_get_num_tuples(ref s);
        }

        public string GetColumnName(ulong index)
        {
            ThrowIfDisposed();
            var s = AsStruct();
            var result = NativeMethods.kuzu_query_result_get_column_name(ref s, index, out var namePtr);
            if (result != KuzuState.Success) throw new KuzuException($"Failed to get column name at index {index}");
            return NativeUtil.PtrToStringAndDestroy(namePtr, NativeMethods.kuzu_destroy_string);
        }

        public DataType GetColumnDataType(ulong index) { ThrowIfDisposed(); var s = AsStruct(); var result = NativeMethods.kuzu_query_result_get_column_data_type(ref s, index, out KuzuLogicalTypeNative dataType); if (result != KuzuState.Success) throw new KuzuException($"Failed to get column data type at index {index}"); return DataType.FromBorrowed(in dataType); }

        public bool HasNext()
        {
            ThrowIfDisposed();
            var s = AsStruct();
            return NativeMethods.kuzu_query_result_has_next(ref s);
        }

        public FlatTuple GetNext()
        {
            ThrowIfDisposed();
            if (!HasNext()) throw new InvalidOperationException("No more tuples available");
            var s = AsStruct();
            var result = NativeMethods.kuzu_query_result_get_next(ref s, out var tupleHandle);
            if (result != KuzuState.Success) throw new KuzuException("Failed to get next tuple");
            tupleHandle.IsOwnedByCpp = true;
            var flatTuple = new FlatTuple(tupleHandle) { Size = GetNumColumns() };
            return flatTuple;
        }

        public bool HasNextQueryResult()
        {
            ThrowIfDisposed();
            var s = AsStruct();
            return NativeMethods.kuzu_query_result_has_next_query_result(ref s);
        }

        public QueryResult GetNextQueryResult()
        {
            ThrowIfDisposed();
            var s = AsStruct();
            var state = NativeMethods.kuzu_query_result_get_next_query_result(ref s, out var next);
            if (state != KuzuState.Success) throw new KuzuException("Failed to get next query result");
            return new QueryResult(next);
        }

        public QuerySummary GetQuerySummary()
        {
            ThrowIfDisposed();
            var s = AsStruct();
            var state = NativeMethods.kuzu_query_result_get_query_summary(ref s, out var summaryHandle);
            if (state != KuzuState.Success) throw new KuzuException("Failed to get query summary");
            return new QuerySummary(summaryHandle);
        }

        public bool TryGetArrowSchema(out ArrowSchema schema)
        {
            ThrowIfDisposed();
            var s = AsStruct();
            var state = NativeMethods.kuzu_query_result_get_arrow_schema(ref s, out schema);
            return state == KuzuState.Success;
        }

        public bool TryGetNextArrowChunk(long chunkSize, out ArrowArray array)
        {
            ThrowIfDisposed();
            var s = AsStruct();
            var state = NativeMethods.kuzu_query_result_get_next_arrow_chunk(ref s, chunkSize, out array);
            return state == KuzuState.Success;
        }

        public override string ToString()
        {
            if (_handle.IsInvalid) return string.Empty;
            var s = AsStruct();
            var strPtr = NativeMethods.kuzu_query_result_to_string(ref s);
            var basic = NativeUtil.PtrToStringAndDestroy(strPtr, NativeMethods.kuzu_destroy_string);
            try { return $"QueryResult(Rows={GetNumTuples()}, Cols={NumColumns})\n" + basic; } catch { return basic; }
        }

        public void ResetIterator()
        {
            ThrowIfDisposed();
            var s = AsStruct();
            NativeMethods.kuzu_query_result_reset_iterator(ref s);
        }

        public void Dispose()
        {
            _handle.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}