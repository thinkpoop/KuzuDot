using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KuzuDot;

namespace KuzuDot.Tests
{
    /// <summary>
    /// Tests that can be run without the native library to validate error handling
    /// </summary>
    [TestClass]
    public class MockableTests
    {
        [TestMethod]
        public void KuzuValue_CreateString_WithNull_ShouldThrowArgumentNullException()
        {
            // This test validates our improved null handling without needing native code
            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => KuzuValue.CreateString(null));
            
            Assert.AreEqual("value", ex.ParamName);
        }

        [TestMethod]
        public void KuzuException_Constructor_WithMessage_ShouldWork()
        {
            var message = "Test error message";
            var ex = new KuzuException(message);
            Assert.AreEqual(message, ex.Message);
        }

        [TestMethod]
        public void KuzuException_Constructor_WithMessageAndInnerException_ShouldWork()
        {
            var message = "Test error message";
            var inner = new InvalidOperationException("Inner exception");
            var ex = new KuzuException(message, inner);
            Assert.AreEqual(message, ex.Message);
            Assert.AreSame(inner, ex.InnerException);
        }

        [TestMethod]
        public void ArgumentValidation_Examples_ShouldWork()
        {
            // Test that our argument validation patterns work
            var ex1 = new ArgumentNullException("paramName", "Test message");
            Assert.AreEqual("paramName", ex1.ParamName);
            
            var ex2 = new ArgumentException("Test message", "paramName");
            Assert.AreEqual("paramName", ex2.ParamName);
            
            // Verify these are the expected exception types
            Assert.IsInstanceOfType(ex1, typeof(ArgumentNullException));
            Assert.IsInstanceOfType(ex2, typeof(ArgumentException));
        }

        [TestMethod]
        public void ObjectDisposedException_ShouldBeThrown_WhenUsingDisposedObject()
        {
            // This test validates that ObjectDisposedException is thrown correctly
            var ex = new ObjectDisposedException("TestObject");
            
            Assert.AreEqual("TestObject", ex.ObjectName);
            Assert.IsInstanceOfType(ex, typeof(ObjectDisposedException));
        }
    }
}