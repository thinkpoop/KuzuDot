using System;
using System.Runtime.InteropServices;
using KuzuDot.Native;
using KuzuDot.Native.Enums;

namespace KuzuDot
{
    /// <summary>
    /// Represents a flat tuple (row) from a query result
    /// </summary>
    public class FlatTuple : IDisposable
    {
        private sealed class FlatTupleSafeHandle : SafeHandle
        {
            internal bool IsOwnedByCpp;
            internal FlatTupleSafeHandle() : base(IntPtr.Zero, true) { }
            public override bool IsInvalid => handle == IntPtr.Zero;
            internal void Initialize(IntPtr ptr) { SetHandle(ptr); }
            protected override bool ReleaseHandle()
            {
                try
                {
                    if (!IsInvalid && !IsOwnedByCpp)
                    {
                        var native = new KuzuFlatTuple { FlatTuple = handle, IsOwnedByCpp = IsOwnedByCpp };
                        NativeMethods.kuzu_flat_tuple_destroy(ref native);
                    }
                    handle = IntPtr.Zero;
                    return true;
                }
                catch { return false; }
            }
        }

        private readonly FlatTupleSafeHandle _handle = new FlatTupleSafeHandle();
        private readonly object _lockObject = new object();

        internal FlatTuple(KuzuFlatTuple native)
        {
            _handle.IsOwnedByCpp = native.IsOwnedByCpp;
            _handle.Initialize(native.FlatTuple);
        }

        /// <summary>
        /// Gets the number of values in this tuple
        /// </summary>
        public ulong Size { get; internal set; }

        /// <summary>
        /// Gets the value at the specified index
        /// </summary>
        /// <param name="index">The zero-based index of the value to retrieve</param>
        /// <returns>A KuzuValue representing the value at the specified index</returns>
        public KuzuValue GetValue(ulong index)
        {
            lock (_lockObject)
            {
                ThrowIfDisposed();
                var native = new KuzuFlatTuple { FlatTuple = _handle.DangerousGetHandle(), IsOwnedByCpp = _handle.IsOwnedByCpp };
                var state = NativeMethods.kuzu_flat_tuple_get_value(ref native, index, out var borrowed);
                if (state != KuzuState.Success) throw new KuzuException($"Failed to get value at index {index}. Native result: {state}");
                if (borrowed.Value == IntPtr.Zero) throw new KuzuException($"Retrieved null handle for value at index {index}");
                borrowed.IsOwnedByCpp = true;
                return KuzuValue.CreateBorrowedFromRaw(borrowed);
            }
        }

        /// <summary>
        /// Converts this flat tuple to a string representation
        /// </summary>
        /// <returns>A string representation of this flat tuple</returns>
        public override string ToString()
        {
            lock (_lockObject)
            {
                if (_handle.IsInvalid) return string.Empty;
                var native = new KuzuFlatTuple { FlatTuple = _handle.DangerousGetHandle(), IsOwnedByCpp = _handle.IsOwnedByCpp };
                var strPtr = NativeMethods.kuzu_flat_tuple_to_string(ref native);
                var row = NativeUtil.PtrToStringAndDestroy(strPtr, NativeMethods.kuzu_destroy_string);
                return $"FlatTuple(Size={Size}) " + row;
            }
        }

        /// <summary>
        /// Dispose this instance
        /// </summary>
        public void Dispose()
        {
            _handle.Dispose();
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed() { if (_handle.IsInvalid) throw new ObjectDisposedException(nameof(FlatTuple)); }
    }
}