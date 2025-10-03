using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KuzuDot; // access public API

namespace KuzuDot.Tests
{
    [TestClass]
    public class KuzuValueTests
    {
        [TestMethod]
        public void CreateNull_IsNullShouldBeTrue_ThenUnset()
        {
            using var v = KuzuValue.CreateNull();
            Assert.IsTrue(v.IsNull());
            v.SetNull(false);
            Assert.IsFalse(v.IsNull());
            v.SetNull(true);
            Assert.IsTrue(v.IsNull());
        }

        [TestMethod]
        public void PrimitiveBool_RoundTrip()
        {
            using var t = KuzuValue.CreateBool(true);
            using var f = KuzuValue.CreateBool(false);
            Assert.AreEqual(true, t.GetBool());
            Assert.AreEqual(false, f.GetBool());
        }

        [TestMethod]
        public void Integers_SignedUnsigned_RoundTrip()
        {
            using var i8 = KuzuValue.CreateInt8(-5);
            using var i16 = KuzuValue.CreateInt16(-32000);
            using var i32 = KuzuValue.CreateInt32(-123456);
            using var i64 = KuzuValue.CreateInt64(long.MaxValue);
            using var u8 = KuzuValue.CreateUInt8(200);
            using var u16 = KuzuValue.CreateUInt16(65000);
            using var u32 = KuzuValue.CreateUInt32(4000000000u);
            using var u64 = KuzuValue.CreateUInt64(18000000000000000000UL);
            Assert.AreEqual(-5, i8.GetInt8());
            Assert.AreEqual(-32000, i16.GetInt16());
            Assert.AreEqual(-123456, i32.GetInt32());
            Assert.AreEqual(long.MaxValue, i64.GetInt64());
            Assert.AreEqual((byte)200, u8.GetUInt8());
            Assert.AreEqual((ushort)65000, u16.GetUInt16());
            Assert.AreEqual(4000000000u, u32.GetUInt32());
            Assert.AreEqual(18000000000000000000UL, u64.GetUInt64());
        }

        [TestMethod]
        public void BigInteger_RoundTrip_Basic()
        {
            var big = BigInteger.Parse("123456789012345678901234567890");
            using var v = KuzuValue.CreateBigInteger(big);
            var back = v.GetBigInteger();
            Assert.AreEqual(big, back);
        }

        [TestMethod]
        public void FloatDouble_RoundTrip_WithTolerance()
        {
            using var f = KuzuValue.CreateFloat(3.14159f);
            using var d = KuzuValue.CreateDouble(2.718281828459045);
            Assert.AreEqual(3.14159f, f.GetFloat(), 0.00001f);
            Assert.AreEqual(2.718281828459045, d.GetDouble(), 1e-12);
        }

        [TestMethod]
        public void DateTimestampInterval_RoundTrip()
        {
            var date = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc);
            var ts = DateTime.UtcNow;
            var span = TimeSpan.FromHours(49) + TimeSpan.FromMilliseconds(123);
            using var dVal = KuzuValue.CreateDate(date);
            using var tsVal = KuzuValue.CreateTimestamp(ts);
            using var intVal = KuzuValue.CreateInterval(span);
            Assert.AreEqual(date.Date, dVal.GetDate().Date);
            Assert.AreEqual(ts.ToLongTimeString(), tsVal.GetTimestampAsDateTime().ToLongTimeString());
            var backSpan = intVal.GetInterval();
            Assert.AreEqual(span.Days, backSpan.Days);
            Assert.AreEqual(span.Hours, backSpan.Hours);
        }

        [TestMethod]
        public void InternalId_RoundTrip()
        {
            var id = new InternalId(7, 999);
            using var v = KuzuValue.CreateInternalId(id);
            var back = v.GetInternalId();
            Assert.AreEqual(id.TableId, back.TableId);
            Assert.AreEqual(id.Offset, back.Offset);
        }

        [TestMethod]
        public void String_EmptyAndRegular_RoundTrip()
        {
            using var empty = KuzuValue.CreateString(string.Empty);
            using var hello = KuzuValue.CreateString("Hello World");
            Assert.AreEqual(string.Empty, empty.GetString());
            Assert.AreEqual("Hello World", hello.GetString());
        }

        [TestMethod]
        public void Clone_ShouldProduceIndependentCopy()
        {
            using var original = KuzuValue.CreateInt32(123);
            using var clone = original.Clone();
            Assert.AreEqual(123, original.GetInt32());
            Assert.AreEqual(123, clone.GetInt32());
            using var newVal = KuzuValue.CreateInt32(456);
            clone.CopyFrom(newVal);
            Assert.AreEqual(123, original.GetInt32());
            Assert.AreEqual(456, clone.GetInt32());
        }

        [TestMethod]
        public void CopyFrom_ShouldOverwriteTargetValue()
        {
            using var target = KuzuValue.CreateInt64(100);
            using var source = KuzuValue.CreateInt64(200);
            target.CopyFrom(source);
            Assert.AreEqual(200L, target.GetInt64());
        }

        [TestMethod]
        public void TypeMismatch_Getter_ShouldThrow()
        {
            using var v = KuzuValue.CreateInt32(10);
            Assert.ThrowsExactly<KuzuException>(() => v.GetDouble());
            Assert.ThrowsExactly<KuzuException>(() => v.GetUInt64());
        }

        [TestMethod]
        public void ToString_ShouldReturnNonEmpty_ForPrimitive()
        {
            using var v = KuzuValue.CreateInt16(42);
            var s = v.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(s));
        }

        [TestMethod]
        public void NullHandling_SetNullTrueThenFalse()
        {
            using var v = KuzuValue.CreateInt8(5);
            Assert.IsFalse(v.IsNull());
            v.SetNull(true);
            Assert.IsTrue(v.IsNull());
            v.SetNull(false);
            Assert.IsFalse(v.IsNull());
        }

        [TestMethod]
        public void Dispose_MultipleCalls_ShouldNotThrow()
        {
            var v = KuzuValue.CreateUInt32(99);
            v.Dispose();
            v.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => v.GetUInt32());
        }
    }
}
