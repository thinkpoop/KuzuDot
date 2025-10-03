using System;

namespace KuzuDot.Utils
{
    internal static class KuzuGuard
    {
        public static void NotNull<T>(T value, string paramName)
        {
            if (value == null) throw new ArgumentNullException(paramName);
        }
        public static void NotNullOrEmpty(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException($"Parameter '{paramName}' cannot be null or empty.", paramName);
        }
    }
}