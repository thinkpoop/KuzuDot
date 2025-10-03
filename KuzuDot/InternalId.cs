using System;
using KuzuDot.Native;

namespace KuzuDot
{
    /// <summary>
    /// Managed wrapper representing an internal identifier (table id + offset) in Kuzu.
    /// This hides the underlying mutable native struct and provides value semantics.
    /// </summary>
    public struct InternalId : IEquatable<InternalId>
    {
        /// <summary>Table identifier.</summary>
        public ulong TableId { get; }
        /// <summary>Offset / row identifier within table.</summary>
        public ulong Offset { get; }

        public InternalId(ulong tableId, ulong offset)
        {
            TableId = tableId;
            Offset = offset;
        }

        internal InternalId(KuzuInternalIdNative native)
        {
            TableId = native.TableId;
            Offset = native.Offset;
        }

        internal KuzuInternalIdNative ToNative() => new KuzuInternalIdNative { TableId = TableId, Offset = Offset };

        public override string ToString() => $"InternalId(Table={TableId}, Offset={Offset})";
        public bool Equals(InternalId other) => TableId == other.TableId && Offset == other.Offset;
        public override bool Equals(object obj) => obj is InternalId o && Equals(o);
        public override int GetHashCode()
        {
            unchecked
            {
                // simple combination suitable for dictionary usage
                ulong combined = (TableId * 397UL) ^ Offset;
                return ((int)(combined >> 32)) ^ (int)combined;
            }
        }
        public static bool operator ==(InternalId left, InternalId right) => left.Equals(right);
        public static bool operator !=(InternalId left, InternalId right) => !left.Equals(right);
    }
}
