using System;
using System.Runtime.InteropServices;
using KuzuDot.Native;

namespace KuzuDot
{
    /// <summary>
    /// Provides version information for the Kuzu library
    /// </summary>
    public static class Version
    {
        /// <summary>
        /// Gets the version string of the Kuzu library
        /// </summary>
        /// <returns>The version string</returns>
        public static string GetVersion()
        {
            var versionPtr = NativeMethods.kuzu_get_version();
            if (versionPtr == IntPtr.Zero)
            {
                return string.Empty;
            }

            try
            {
                // Marshal the string pointer to a managed string
                var versionString = Marshal.PtrToStringAnsi(versionPtr);

                // Check if the marshaled string is null
                if (versionString == null)
                {
                    throw new InvalidOperationException("Failed to marshal version string.");
                }

                return versionString;
            }
            finally
            {
                // Always destroy the native string, regardless of whether an exception occurred
                NativeMethods.kuzu_destroy_string(versionPtr);
            }
        }

        /// <summary>
        /// Gets the storage version of the Kuzu library
        /// </summary>
        /// <returns>The storage version number</returns>
        public static ulong GetStorageVersion()
        {
            return NativeMethods.kuzu_get_storage_version();
        }
    }
}