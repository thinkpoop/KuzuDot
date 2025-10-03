using System;
using System.Runtime.InteropServices;
using System.Numerics;
using KuzuDot.Native.Enums;

namespace KuzuDot.Native
{
    /// <summary>
    /// Utility class for working with BigInteger via native 128-bit support.
    /// </summary>
    internal static class Int128Utilities
    {
        internal static KuzuInt128 FromBigInteger(BigInteger value)
        {
            var bytes = value.ToByteArray();
            if (bytes.Length > 16) throw new OverflowException("BigInteger does not fit into 128 bits");
            byte[] padded = new byte[16];
            byte fill = value.Sign < 0 ? (byte)0xFF : (byte)0x00;
            for (int i = 0; i < 16; i++) padded[i] = fill;
            Array.Copy(bytes, 0, padded, 0, bytes.Length);
            ulong low = BitConverter.ToUInt64(padded, 0);
            long high = BitConverter.ToInt64(padded, 8);
            return new KuzuInt128 { Low = low, High = high };
        }

        internal static BigInteger ToBigInteger(KuzuInt128 native)
        {
            byte[] bytes = new byte[16];
            Array.Copy(BitConverter.GetBytes(native.Low), 0, bytes, 0, 8);
            Array.Copy(BitConverter.GetBytes(native.High), 0, bytes, 8, 8);
            return new BigInteger(bytes); // two's complement
        }

        internal static string ToString(KuzuInt128 value)
        {
            var result = NativeMethods.kuzu_int128_t_to_string(value, out var strPtr);
            if (result != KuzuState.Success || strPtr == IntPtr.Zero)
            {
                throw new KuzuException("Failed to convert Int128 to string");
            }
            try { return Marshal.PtrToStringAnsi(strPtr) ?? string.Empty; }
            finally { NativeMethods.kuzu_destroy_string(strPtr); }
        }

        internal static bool TryParse(string str, out KuzuInt128 value)
        {
            value = default;
            if (string.IsNullOrEmpty(str)) return false;
            var res = NativeMethods.kuzu_int128_t_from_string(str, out value);
            return res == KuzuState.Success;
        }
    }
}