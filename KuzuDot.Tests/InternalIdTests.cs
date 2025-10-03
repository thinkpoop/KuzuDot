using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KuzuDot.Tests
{
    [TestClass]
    public class InternalIdTests
    {
        [TestMethod]
        public void PublicValueStructs_DefaultValues()
        {
            var internalId = new InternalId(); // default ctor -> fields default to 0
            Assert.AreEqual(0ul, internalId.TableId);
            Assert.AreEqual(0ul, internalId.Offset);
        }

        [TestMethod]
        public void PublicValueStructs_Assignment()
        {
            var id = new InternalId(5, 99);
            Assert.AreEqual((ulong)5, id.TableId);
            Assert.AreEqual((ulong)99, id.Offset);
            Assert.AreEqual("InternalId(Table=5, Offset=99)", id.ToString());
        }
    }
}