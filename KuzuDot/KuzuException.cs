using System;

namespace KuzuDot
{
    public class KuzuException : Exception
    {
        public KuzuException(string message) : base(message) { }
        public KuzuException(string message, Exception innerException) : base(message, innerException) { }
    }
}