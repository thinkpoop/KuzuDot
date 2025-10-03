using System;
using System.Runtime.InteropServices;
using KuzuDot.Native;
using KuzuDot.Native.Enums;
using KuzuDot.Utils;

namespace KuzuDot
{
    public sealed class DatabaseConfig
    {
        public ulong BufferPoolSize { get; set; }
        public ulong MaxNumThreads { get; set; }
        public bool EnableCompression { get; set; }
        public bool ReadOnly { get; set; }
        public ulong MaxDbSize { get; set; }
        public bool AutoCheckpoint { get; set; }
        public ulong CheckpointThreshold { get; set; }

        internal KuzuSystemConfig ToNative() => new KuzuSystemConfig
        {
            BufferPoolSize = BufferPoolSize,
            MaxNumThreads = MaxNumThreads,
            EnableCompression = EnableCompression,
            ReadOnly = ReadOnly,
            MaxDbSize = MaxDbSize,
            AutoCheckpoint = AutoCheckpoint,
            CheckpointThreshold = CheckpointThreshold
        };

        public static DatabaseConfig Default()
        {
            var n = NativeMethods.kuzu_default_system_config();
            return new DatabaseConfig
            {
                BufferPoolSize = n.BufferPoolSize,
                MaxNumThreads = n.MaxNumThreads,
                EnableCompression = n.EnableCompression,
                ReadOnly = n.ReadOnly,
                MaxDbSize = n.MaxDbSize,
                AutoCheckpoint = n.AutoCheckpoint,
                CheckpointThreshold = n.CheckpointThreshold
            };
        }
        public override string ToString() => $"DatabaseConfig(BP={BufferPoolSize}, Threads={MaxNumThreads}, ReadOnly={ReadOnly})";
    }

    /// <summary>
    /// Represents a Kuzu database instance.
    /// Multiple connections can be created from the same database instance safely.
    /// </summary>
    public class Database : IDisposable
    {
        private sealed class DatabaseSafeHandle : SafeHandle
        {
            internal DatabaseSafeHandle() : base(IntPtr.Zero, true) { }
            public override bool IsInvalid => handle == IntPtr.Zero;
            internal void Initialize(IntPtr ptr) { SetHandle(ptr); }
            protected override bool ReleaseHandle()
            {
                try
                {
                    if (!IsInvalid)
                    {
                        var native = new KuzuDatabase { Database = handle };
                        NativeMethods.kuzu_database_destroy(ref native);
                        handle = IntPtr.Zero;
                    }
                    return true;
                }
                catch { return false; }
            }
        }

        private readonly DatabaseSafeHandle _handle = new DatabaseSafeHandle();
        private readonly string _path;

        /// <summary>
        /// Initializes a new database instance at the specified path with default configuration.
        /// </summary>
        /// <param name="path">The file system path where the database is located or will be created; ":memory:" or an empty string will create an in-memory database.</param>
        /// <exception cref="ArgumentException">Thrown when path is null or whitespace.</exception>
        /// <exception cref="KuzuException">Thrown when database initialization fails.</exception>
        public Database(string path) : this(path, DatabaseConfig.Default()) { }

        /// <summary>
        /// Initializes a new database instance at the specified path.
        /// </summary>
        /// <param name="path">The file system path where the database is located or will be created; ":memory:" or an empty string will create an in-memory database.</param>
        /// <param name="config">System configuration.</param>
        /// <exception cref="ArgumentException">Thrown when path is null or whitespace.</exception>
        /// <exception cref="KuzuException">Thrown when database initialization fails.</exception>
        public Database(string path, DatabaseConfig config)
        {
            KuzuGuard.NotNull(path, nameof(path));
            KuzuGuard.NotNull(config, nameof(config));
            _path = path;
            try
            {
                var state = NativeMethods.kuzu_database_init(path, config.ToNative(), out var nativeDb);
                if (state != KuzuState.Success || nativeDb.Database == IntPtr.Zero)
                    throw new KuzuException($"Failed to initialize database at path: {path}");
                _handle.Initialize(nativeDb.Database);
            }
            catch (DllNotFoundException ex)
            {
                throw new KuzuException($"Native Kuzu library (kuzu_shared.dll) not found. {ex.Message}", ex);
            }
            catch (BadImageFormatException ex)
            {
                throw new KuzuException($"Invalid native library format (architecture mismatch). {ex.Message}", ex);
            }
        }

        internal IntPtr Handle
        {
            get
            {
                ThrowIfDisposed();
                return _handle.DangerousGetHandle();
            }
        }

        /// <summary>
        /// Creates a new connection to this database.
        /// Multiple connections can be created and used concurrently.
        /// </summary>
        /// <returns>A new connection instance.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the database has been disposed.</exception>
        /// <exception cref="KuzuException">Thrown when connection creation fails.</exception>
        public Connection Connect()
        {
            ThrowIfDisposed();
            return new Connection(this);
        }

        public override string ToString() => _handle.IsInvalid ? "Database(Disposed)" : $"Database(Path={_path ?? ""})";

        /// <summary>
        /// Releases all resources used by the Database.
        /// </summary>
        public void Dispose()
        {
            _handle.Dispose();
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed() { if (_handle.IsInvalid) throw new ObjectDisposedException(nameof(Database)); }
    }
}