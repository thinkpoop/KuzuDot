using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KuzuDot;

namespace KuzuDot.Tests
{
    [TestClass]
    public class KuzuExceptionTests
    {
        [TestMethod]
        public void Constructor_WithMessage_ShouldSetMessage()
        {
            // Arrange
            string expectedMessage = "Test exception message";
            
            // Act
            var exception = new KuzuException(expectedMessage);
            
            // Assert
            Assert.AreEqual(expectedMessage, exception.Message);
        }

        [TestMethod]
        public void Constructor_WithMessageAndInnerException_ShouldSetBoth()
        {
            // Arrange
            string expectedMessage = "Test exception message";
            var innerException = new InvalidOperationException("Inner exception");
            
            // Act
            var exception = new KuzuException(expectedMessage, innerException);
            
            // Assert
            Assert.AreEqual(expectedMessage, exception.Message);
            Assert.AreEqual(innerException, exception.InnerException);
        }

        [TestMethod]
        public void Constructor_WithNullMessage_ShouldNotThrow()
        {
            // Act & Assert
            var exception = new KuzuException(null!);
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        public void Constructor_WithEmptyMessage_ShouldNotThrow()
        {
            // Act & Assert
            var exception = new KuzuException(string.Empty);
            Assert.IsNotNull(exception);
            Assert.AreEqual(string.Empty, exception.Message);
        }

        [TestMethod]
        public void Throw_ShouldBeInstanceOfException()
        {
            // Arrange
            var ex = new KuzuException("test");
            
            // Assert
            Assert.IsInstanceOfType(ex, typeof(Exception));
        }
    }
}