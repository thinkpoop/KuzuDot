using System;
using System.Numerics;

namespace KuzuDot
{
    /// <summary>
    /// Generic strongly-typed wrapper around <see cref="KuzuDot.KuzuValue"/> providing
    /// compile-time type safety for a subset of supported primitive/value types.
    /// This class does NOT replace <see cref="KuzuDot.KuzuValue"/>; it layers on top of it.
    /// </summary>
    /// <typeparam name="T">A supported Kuzu value type.</typeparam>
    public sealed class KuzuValue<T> : IDisposable
    {
        private readonly KuzuValue _inner;

        private KuzuValue(KuzuValue inner) { _inner = inner ?? throw new ArgumentNullException(nameof(inner)); }

        /// <summary>Underlying non-generic value.</summary>
        public KuzuValue Inner => _inner;

        /// <summary>Returns true if underlying value is null.</summary>
        public bool IsNull => _inner.IsNull();

        /// <summary>Marks underlying value null/non-null.</summary>
        public void SetNull(bool isNull) => _inner.SetNull(isNull);

        /// <summary>Gets the logical data type from the underlying value.</summary>
        public DataType GetDataType() => _inner.GetDataType();

        /// <summary>The typed value (throws if the underlying native type does not match T).</summary>
        public T Value => KuzuValueTypeMap<T>.Getter(_inner);

        /// <summary>Create a generic KuzuValue from a managed value.</summary>
        public static KuzuValue<T> From(T value) => new KuzuValue<T>(KuzuValueTypeMap<T>.Creator(value));

        /// <summary>Convenience implicit conversion from managed value to generic KuzuValue.</summary>
        public static implicit operator KuzuValue<T>(T value) => From(value);

        /// <summary>Convenience implicit conversion extracting the managed value.</summary>
        public static implicit operator T(KuzuValue<T> value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return value.Value;
        }

        public override string ToString() => _inner.ToString();

        public void Dispose() => _inner.Dispose();
    }

    /// <summary>
    /// Internal mapping between CLR types and factory/getter delegates on the non-generic KuzuValue.
    /// Kept separate to avoid JIT expanding large switch logic inside each generic instantiation.
    /// </summary>
    /// <typeparam name="T">CLR type.</typeparam>
    internal static class KuzuValueTypeMap<T>
    {
        internal static readonly Func<T, KuzuValue> Creator;
        internal static readonly Func<KuzuValue, T> Getter;

        static KuzuValueTypeMap()
        {
            // bool
            if (typeof(T) == typeof(bool))
            {
                Creator = v => KuzuValue.CreateBool((bool)(object)v);
                Getter = kv => (T)(object)kv.GetBool();
                return;
            }
            // signed ints
            if (typeof(T) == typeof(sbyte)) { Creator = v => KuzuValue.CreateInt8((sbyte)(object)v); Getter = kv => (T)(object)kv.GetInt8(); return; }
            if (typeof(T) == typeof(short)) { Creator = v => KuzuValue.CreateInt16((short)(object)v); Getter = kv => (T)(object)kv.GetInt16(); return; }
            if (typeof(T) == typeof(int)) { Creator = v => KuzuValue.CreateInt32((int)(object)v); Getter = kv => (T)(object)kv.GetInt32(); return; }
            if (typeof(T) == typeof(long)) { Creator = v => KuzuValue.CreateInt64((long)(object)v); Getter = kv => (T)(object)kv.GetInt64(); return; }
            // unsigned ints
            if (typeof(T) == typeof(byte)) { Creator = v => KuzuValue.CreateUInt8((byte)(object)v); Getter = kv => (T)(object)kv.GetUInt8(); return; }
            if (typeof(T) == typeof(ushort)) { Creator = v => KuzuValue.CreateUInt16((ushort)(object)v); Getter = kv => (T)(object)kv.GetUInt16(); return; }
            if (typeof(T) == typeof(uint)) { Creator = v => KuzuValue.CreateUInt32((uint)(object)v); Getter = kv => (T)(object)kv.GetUInt32(); return; }
            if (typeof(T) == typeof(ulong)) { Creator = v => KuzuValue.CreateUInt64((ulong)(object)v); Getter = kv => (T)(object)kv.GetUInt64(); return; }
            // floating point
            if (typeof(T) == typeof(float)) { Creator = v => KuzuValue.CreateFloat((float)(object)v); Getter = kv => (T)(object)kv.GetFloat(); return; }
            if (typeof(T) == typeof(double)) { Creator = v => KuzuValue.CreateDouble((double)(object)v); Getter = kv => (T)(object)kv.GetDouble(); return; }
            // BigInteger
            if (typeof(T) == typeof(BigInteger)) { Creator = v => KuzuValue.CreateBigInteger((BigInteger)(object)v); Getter = kv => (T)(object)kv.GetBigInteger(); return; }
            // string
            if (typeof(T) == typeof(string)) { Creator = v => KuzuValue.CreateString((string)(object)v); Getter = kv => (T)(object)kv.GetString(); return; }
            // DateTime (treat as Timestamp by default) - users wanting Date-only should call utility on non-generic API.
            if (typeof(T) == typeof(DateTime))
            {
                Creator = v => KuzuValue.CreateTimestamp((DateTime)(object)v);
                Getter = kv => (T)(object)kv.GetTimestampAsDateTime();
                return;
            }
            // InternalId wrapper
            if (typeof(T) == typeof(InternalId))
            {
                Creator = v => KuzuValue.CreateInternalId((InternalId)(object)v);
                Getter = kv => (T)(object)kv.GetInternalId();
                return;
            }

            // Unsupported -> throw early when attempting to use
            Creator = _ => throw new NotSupportedException($"Type {typeof(T)} is not supported by KuzuValue<T>.");
            Getter = _ => throw new NotSupportedException($"Type {typeof(T)} is not supported by KuzuValue<T>.");
        }
    }
}
