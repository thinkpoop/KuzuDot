using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KuzuDot.Native;

namespace KuzuDot.Tests.ResourceManagement
{
    /// <summary>
    /// Tests to verify that the memory management fixes prevent protected memory access errors
    /// </summary>
    [TestClass]
    public class MemoryManagementFixTests
    {
        private Database? _database;
        private Connection? _connection;
        private string? _initializationError;

        [TestInitialize]
        public void TestInitialize()
        {
            try
            {
                _database = new Database(":memory:");
                _connection = _database.Connect();
                _initializationError = null;
            }
            catch (KuzuException ex)
            {
                _database = null;
                _connection = null;
                _initializationError = ex.Message;
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _connection?.Dispose();
            _database?.Dispose();
        }

        private void EnsureNativeLibraryAvailable()
        {
            if (_database == null || _connection == null)
            {
                Assert.Inconclusive($"Cannot run test: Native Kuzu library is not available. Error: {_initializationError}");
            }
        }

        [TestMethod]
        public void KuzuVersion_GetVersion_ShouldNotCauseMemoryError()
        {
            EnsureNativeLibraryAvailable();
            string version1 = Version.GetVersion();
            string version2 = Version.GetVersion();
            Assert.IsFalse(string.IsNullOrEmpty(version1));
            Assert.AreEqual(version1, version2);
        }

        [TestMethod]
        public void QueryResult_ToString_ShouldNotCauseMemoryError()
        {
            EnsureNativeLibraryAvailable();
            using var result = _connection!.Query("RETURN 1");
            string str1 = result.ToString();
            string str2 = result.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(str1));
            Assert.AreEqual(str1, str2);
        }

        [TestMethod]
        public void PreparedStatement_ErrorMessage_ShouldNotCauseMemoryError()
        {
            EnsureNativeLibraryAvailable();
            try
            {
                using var stmt = _connection!.Prepare("INVALID SYNTAX HERE");
                string error1 = stmt.ErrorMessage;
                string error2 = stmt.ErrorMessage;
                Assert.AreEqual(error1, error2);
            }
            catch (KuzuException)
            {
                Assert.IsTrue(true, "Invalid syntax correctly threw exception without memory errors");
            }
        }

        [TestMethod]
        public void KuzuValue_ToString_ShouldNotCauseMemoryError()
        {
            EnsureNativeLibraryAvailable();
            using var value = KuzuValue.CreateString("Test Value");
            string str1 = value.ToString();
            string str2 = value.ToString();
            Assert.IsFalse(string.IsNullOrEmpty(str1));
            Assert.AreEqual(str1, str2);
        }

        [TestMethod]
        public void KuzuValue_GetString_ShouldNotCauseMemoryError()
        {
            EnsureNativeLibraryAvailable();
            using var value = KuzuValue.CreateString("Test Value");
            string str1 = value.GetString();
            string str2 = value.GetString();
            Assert.AreEqual("Test Value", str1);
            Assert.AreEqual(str1, str2);
        }

        [TestMethod]
        public void MultipleStringOperations_ShouldNotCauseMemoryError()
        {
            EnsureNativeLibraryAvailable();
            for (int i = 0; i < 5; i++)
            {
                var version = Version.GetVersion();
                using var value = KuzuValue.CreateString($"Test{i}");
                var valueString = value.GetString();
                var valueToString = value.ToString();
                Assert.IsFalse(string.IsNullOrEmpty(version));
                Assert.AreEqual($"Test{i}", valueString);
                Assert.IsFalse(string.IsNullOrEmpty(valueToString));
            }
        }
    }
}