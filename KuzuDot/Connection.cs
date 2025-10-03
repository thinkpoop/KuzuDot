using System;
using System.Runtime.InteropServices;
using KuzuDot.Native;
using KuzuDot.Native.Enums;
using KuzuDot.Utils;

namespace KuzuDot
{
    /// <summary>
    /// Represents a connection to a Kuzu database.
    /// </summary>
    public class Connection : IDisposable
    {
        private sealed class ConnectionSafeHandle : SafeHandle
        {
            private readonly IntPtr _dbPtr; // retained for potential future validation/logging
            internal ConnectionSafeHandle(IntPtr dbPtr) : base(IntPtr.Zero, true) { _dbPtr = dbPtr; }
            public override bool IsInvalid => handle == IntPtr.Zero;
            internal void Initialize(IntPtr ptr) { SetHandle(ptr); }
            protected override bool ReleaseHandle()
            {
                try
                {
                    if (!IsInvalid)
                    {
                        var nativeConn = new KuzuConnection { Connection = handle };
                        NativeMethods.kuzu_connection_destroy(ref nativeConn);
                        handle = IntPtr.Zero;
                    }
                    return true;
                }
                catch { return false; }
            }
        }

        private readonly ConnectionSafeHandle _handle;

        internal Connection(Database database)
        {
            KuzuGuard.NotNull(database, nameof(database));
            _handle = new ConnectionSafeHandle(database.Handle); // database provides IntPtr internally
            var dbStruct = new KuzuDatabase { Database = database.Handle };
            var state = NativeMethods.kuzu_connection_init(ref dbStruct, out var nativeConn);
            if (state != KuzuState.Success || nativeConn.Connection == IntPtr.Zero)
                throw new KuzuException("Failed to create connection");
            _handle.Initialize(nativeConn.Connection);
        }

        private void ThrowIfInvalid()
        {
            if (_handle.IsInvalid)
                throw new ObjectDisposedException(nameof(Connection));
        }

        private KuzuConnection GetNativeConnection()
        {
            ThrowIfInvalid();
            return new KuzuConnection { Connection = _handle.DangerousGetHandle() };
        }

        /// <summary>
        /// Gets or sets the maximum number of threads to use for executing queries.
        /// </summary>
        public ulong MaxNumThreadsForExecution
        {
            get
            {
                var conn = GetNativeConnection();
                var state = NativeMethods.kuzu_connection_get_max_num_thread_for_exec(ref conn, out var n);
                if (state != KuzuState.Success) throw new KuzuException("Failed to get maximum number of threads for execution");
                return n;
            }
            set
            {
                var conn = GetNativeConnection();
                var state = NativeMethods.kuzu_connection_set_max_num_thread_for_exec(ref conn, value);
                if (state != KuzuState.Success) throw new KuzuException("Failed to set maximum number of threads for execution");
            }
        }

        /// <summary>
        /// Sets the query timeout value in milliseconds for this connection.
        /// </summary>
        public void SetQueryTimeout(ulong timeoutMs)
        {
            var conn = GetNativeConnection();
            var state = NativeMethods.kuzu_connection_set_query_timeout(ref conn, timeoutMs);
            if (state != KuzuState.Success) throw new KuzuException("Failed to set query timeout");
        }

        /// <summary>
        /// Interrupts the current query execution in this connection.
        /// </summary>
        public void Interrupt()
        {
            var conn = GetNativeConnection();
            NativeMethods.kuzu_connection_interrupt(ref conn);
        }

        /// <summary>
        /// Executes a query and returns the result.
        /// </summary>
        public QueryResult Query(string query)
        {
            KuzuGuard.NotNullOrEmpty(query, nameof(query));
            var conn = GetNativeConnection();
            var state = NativeMethods.kuzu_connection_query(ref conn, query, out var qr);
            if (state != KuzuState.Success) throw new KuzuException($"Failed to execute query: {query}");
            return new QueryResult(qr);
        }

        /// <summary>
        /// Prepares a statement for execution.
        /// </summary>
        public PreparedStatement Prepare(string query)
        {
            KuzuGuard.NotNullOrEmpty(query, nameof(query));
            var conn = GetNativeConnection();
            var state = NativeMethods.kuzu_connection_prepare(ref conn, query, out var ps);
            return new PreparedStatement(ps, this);
        }

        internal QueryResult Execute(PreparedStatement preparedStatement)
        {
            KuzuGuard.NotNull(preparedStatement, nameof(preparedStatement));
            var conn = GetNativeConnection();
            ref var psStruct = ref preparedStatement.NativeStruct;
            var state = NativeMethods.kuzu_connection_execute(ref conn, ref psStruct, out var qr);
            if (state != KuzuState.Success)
            {
                string details = string.Empty;
                try { details = preparedStatement.ErrorMessage; if (!string.IsNullOrEmpty(details)) details = " Details: " + details; } catch { }
                throw new KuzuException("Failed to execute prepared statement." + details);
            }
            return new QueryResult(qr);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString() => _handle.IsInvalid ? "Connection(Disposed)" : "Connection(Ptr=0x" + _handle.DangerousGetHandle().ToString("X") + ")";

        /// <summary>
        /// Releases all resources used by the Connection.
        /// </summary>
        public void Dispose()
        {
            _handle.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}