using System;

namespace KuzuDot.Native
{
    internal static class NativeUtil
    {
        public static string PtrToStringAndDestroy(IntPtr ptr, Action<IntPtr> destroy)
        {
            if (ptr == IntPtr.Zero) return string.Empty;
            try { return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr) ?? string.Empty; }
            finally { destroy(ptr); }
        }
    }
}