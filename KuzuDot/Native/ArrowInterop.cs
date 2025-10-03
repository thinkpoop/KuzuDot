using System;
using System.Runtime.InteropServices;

namespace KuzuDot.Native
{
    // Minimal Arrow C Data Interface structs for interop exposure.
    // These are public because QueryResult exposes them in TryGet* methods.
    [StructLayout(LayoutKind.Sequential)]
    public struct ArrowSchema
    {
        public IntPtr format;      // const char*
        public IntPtr name;        // const char*
        public IntPtr metadata;    // const char*
        public long flags;
        public long n_children;
        public IntPtr children;    // ArrowSchema**
        public IntPtr dictionary;  // ArrowSchema*
        public IntPtr release;     // void (*release)(ArrowSchema*)
        public IntPtr private_data; // void*
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ArrowArray
    {
        public long length;
        public long null_count;
        public long offset;
        public long n_buffers;
        public long n_children;
        public IntPtr buffers;     // const void**
        public IntPtr children;    // ArrowArray**
        public IntPtr dictionary;  // ArrowArray*
        public IntPtr release;     // void (*release)(ArrowArray*)
        public IntPtr private_data; // void*
    }
}
