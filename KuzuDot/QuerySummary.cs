using System;
using System.Runtime.InteropServices;
using KuzuDot.Native;

namespace KuzuDot
{
    /// <summary>
    /// Managed wrapper for Kuzu query summary (compilation & execution timing info).
    /// </summary>
    public sealed class QuerySummary : IDisposable
    {
        private sealed class QuerySummarySafeHandle : SafeHandle
        {
            internal QuerySummarySafeHandle() : base(IntPtr.Zero, true) { }
            public override bool IsInvalid => handle == IntPtr.Zero;
            internal void Initialize(IntPtr ptr) { SetHandle(ptr); }
            protected override bool ReleaseHandle()
            {
                try
                {
                    if (!IsInvalid)
                    {
                        var native = new KuzuQuerySummary { QuerySummary = handle };
                        NativeMethods.kuzu_query_summary_destroy(ref native);
                    }
                    handle = IntPtr.Zero;
                    return true;
                }
                catch { return false; }
            }
        }

        private readonly QuerySummarySafeHandle _handle = new QuerySummarySafeHandle();

        internal QuerySummary(KuzuQuerySummary native)
        {
            _handle.Initialize(native.QuerySummary);
        }

        public double CompilingTimeMs
        {
            get
            {
                ThrowIfDisposed();
                var native = new KuzuQuerySummary { QuerySummary = _handle.DangerousGetHandle() };
                return NativeMethods.kuzu_query_summary_get_compiling_time(ref native);
            }
        }

        public double ExecutionTimeMs
        {
            get
            {
                ThrowIfDisposed();
                var native = new KuzuQuerySummary { QuerySummary = _handle.DangerousGetHandle() };
                return NativeMethods.kuzu_query_summary_get_execution_time(ref native);
            }
        }

        public override string ToString()
        {
            if (_handle.IsInvalid) return "QuerySummary(Disposed)";
            return $"QuerySummary(CompileMs={CompilingTimeMs:F2}, ExecMs={ExecutionTimeMs:F2})";
        }

        private void ThrowIfDisposed() { if (_handle.IsInvalid) throw new ObjectDisposedException(nameof(QuerySummary)); }

        public void Dispose()
        {
            _handle.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
