using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KuzuDot;

namespace KuzuDot.Tests
{
    [TestClass]
    public class DatabaseTests
    {
        private string _testDbPath = null!;

        [TestInitialize]
        public void TestInitialize()
        {
            _testDbPath = Path.GetTempFileName();
            File.Delete(_testDbPath); // Remove the temp file so we can create a directory
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(_testDbPath))
            {
                Directory.Delete(_testDbPath, true);
            }
        }

        [TestMethod]
        public void Constructor_WithValidPath_ShouldCreateDatabase()
        {
            // Arrange & Act
            using var database = new Database(_testDbPath);
            
            // Assert
            Assert.IsNotNull(database);
        }

        [TestMethod]
        public void Constructor_WithInvalidPath_ShouldThrowException()
        {
            // Arrange
            string invalidPath = "/invalid/path/that/does/not/exist";
            
            // Act & Assert
            Assert.ThrowsExactly<KuzuException>(() => new Database(invalidPath));
        }

        [TestMethod]
        public void Constructor_WithInMemoryPath_ShouldCreateInMemoryDatabase()
        {
            // Arrange & Act
            using var database = new Database(":memory:");
            
            // Assert
            Assert.IsNotNull(database);
        }

        [TestMethod]
        public void Connect_ShouldReturnValidConnection()
        {
            // Arrange
            using var database = new Database(":memory:");
            
            // Act
            using var connection = database.Connect();
            
            // Assert
            Assert.IsNotNull(connection);
        }

        [TestMethod]
        public void Connect_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var database = new Database(":memory:");
            database.Dispose();
            
            // Act & Assert
            Assert.ThrowsExactly<ObjectDisposedException>(() => database.Connect());
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var database = new Database(":memory:");
            
            // Act & Assert
            database.Dispose(); // Should not throw
            database.Dispose(); // Second call should also not throw
        }
    }
}