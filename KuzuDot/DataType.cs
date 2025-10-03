using System;
using KuzuDot.Native;
using KuzuDot.Native.Enums;

namespace KuzuDot
{
    /// <summary>
    /// Managed wrapper around a logical (data) type in the native engine.
    /// </summary>
    public sealed class DataType : IDisposable, IEquatable<DataType>
    {
        private KuzuLogicalTypeNative _native;
        private bool _disposed;
        internal DataType(KuzuLogicalTypeNative native) { _native = native; }

        /// <summary>Creates a cloned copy of the underlying native logical type.</summary>
        internal static DataType FromBorrowed(in KuzuLogicalTypeNative native)
        {
            var copySrc = native; // need mutable ref for P/Invoke
            NativeMethods.kuzu_data_type_clone(ref copySrc, out var clone);
            return new DataType(clone);
        }

        /// <summary>Raw underlying id (engine specific).</summary>
        public uint Id
        {
            get
            {
                ThrowIfDisposed();
                var tmp = _native; // pass by ref
                var id = NativeMethods.kuzu_data_type_get_id(ref tmp);
                return (uint)id;
            }
        }

        /// <summary>If this represents an ARRAY type returns element count, else null.</summary>
        public ulong? ArrayNumElements
        {
            get
            {
                ThrowIfDisposed();
                var tmp = _native;
                var state = NativeMethods.kuzu_data_type_get_num_elements_in_array(ref tmp, out var n);
                return state == KuzuState.Success ? (ulong?)n : null;
            }
        }

        public DataType Clone()
        {
            ThrowIfDisposed();
            var tmp = _native;
            NativeMethods.kuzu_data_type_clone(ref tmp, out var clone);
            return new DataType(clone);
        }

        public bool Equals(DataType other)
        {
            if (other == null) return false;
            ThrowIfDisposed(); other.ThrowIfDisposed();
            var left = _native; var right = other._native;
            return NativeMethods.kuzu_data_type_equals(ref left, ref right);
        }

        public override bool Equals(object obj) => obj is DataType dt && Equals(dt);
        public override int GetHashCode() => (int)Id;
        public static bool operator ==(DataType a, DataType b) => ReferenceEquals(a, b) || (a?.Equals(b) ?? false);
        public static bool operator !=(DataType a, DataType b) => !(a == b);

        public override string ToString()
        {
            if (_disposed) return "DataType(Disposed)";
            return "DataType(Id=" + Id + (ArrayNumElements is ulong n ? ",Elements=" + n : string.Empty) + ")";
        }

        private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(DataType)); }

        public void Dispose()
        {
            if (!_disposed)
            {
                try { NativeMethods.kuzu_data_type_destroy(ref _native); } catch { }
                _native.DataType = IntPtr.Zero;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
