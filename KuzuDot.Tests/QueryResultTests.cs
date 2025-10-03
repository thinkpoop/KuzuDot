using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KuzuDot;
using KuzuDot.Native;

namespace KuzuDot.Tests
{
    [TestClass]
    public class QueryResultTests
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
                // Native library not available in test environment
                _database = null;
                _connection = null;
                _initializationError = ex.Message;
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Dispose in correct order: connection first, then database
            _connection?.Dispose();
            _database?.Dispose();
            
            // Force garbage collection to ensure native resources are cleaned up
            // This is critical for test isolation when running in parallel
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void EnsureNativeLibraryAvailable()
        {
            if (_database == null || _connection == null)
            {
                throw new InvalidOperationException($"Cannot run test: Native Kuzu library is not available. Error: {_initializationError}");
            }
        }

        [TestMethod]
        public void IsSuccess_ShouldReturnTrue_ForSuccessfulQuery()
        {
            // Ensure native library is available
            EnsureNativeLibraryAvailable();

            // Arrange & Act
            using var result = _connection!.Query("CREATE NODE TABLE Person(name STRING, PRIMARY KEY(name));");
            
            // Assert
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void NumColumns_ShouldReturnCorrectCount_ForSelectQuery()
        {
            // Ensure native library is available
            EnsureNativeLibraryAvailable();

            // Arrange
            using var createResult = _connection!.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));");
            using var insertResult = _connection.Query("CREATE (:Person {name: 'Alice', age: 30});");
            
            // Act
            using var result = _connection.Query("MATCH (p:Person) RETURN p.name, p.age;");
            
            // Assert
            Assert.AreEqual(2UL, result.NumColumns);
        }

        [TestMethod]
        public void HasNext_ShouldReturnTrue_WhenResultsExist()
        {
            // Ensure native library is available
            EnsureNativeLibraryAvailable();

            // Arrange
            using var createResult = _connection!.Query("CREATE NODE TABLE Person(name STRING, PRIMARY KEY(name));");
            using var insertResult = _connection.Query("CREATE (:Person {name: 'Bob'});");
            
            // Act
            using var result = _connection.Query("MATCH (p:Person) RETURN p.name;");
            
            // Assert
            Assert.IsTrue(result.HasNext());
        }

        [TestMethod]
        public void HasNext_ShouldReturnFalse_WhenNoResultsExist()
        {
            // Ensure native library is available
            EnsureNativeLibraryAvailable();

            // Arrange
            using var createResult = _connection!.Query("CREATE NODE TABLE Person(name STRING, PRIMARY KEY(name));");
            
            // Act
            using var result = _connection.Query("MATCH (p:Person) RETURN p.name;");
            
            // Assert
            Assert.IsFalse(result.HasNext());
        }

        [TestMethod]
        public void HasNext_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Ensure native library is available
            EnsureNativeLibraryAvailable();

            // Arrange
            using var createResult = _connection!.Query("CREATE NODE TABLE Person(name STRING, PRIMARY KEY(name));");
            var result = _connection.Query("CREATE NODE TABLE AnotherTable(id INT64, PRIMARY KEY(id));");
            result.Dispose();
            
            // Act & Assert
            Assert.ThrowsExactly<ObjectDisposedException>(() => result.HasNext());
        }

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            // Ensure native library is available
            EnsureNativeLibraryAvailable();

            // Arrange
            var result = _connection!.Query("CREATE NODE TABLE Person(name STRING, PRIMARY KEY(name));");
            
            // Act & Assert
            result.Dispose(); // Should not throw
            result.Dispose(); // Second call should also not throw
        }

        [TestMethod]
        public void Constructor_WithFailedQuery_ShouldThrowException()
        {
            // Ensure native library is available
            EnsureNativeLibraryAvailable();

            // Act & Assert
            Assert.ThrowsExactly<KuzuException>(() => _connection!.Query("INVALID QUERY SYNTAX"));
        }

        [TestMethod]
        public void QuerySummary_ShouldReturnTimings()
        {
            EnsureNativeLibraryAvailable();
            using var create = _connection!.Query("CREATE NODE TABLE T(id INT64, PRIMARY KEY(id));");
            using var insert = _connection.Query("CREATE (:T {id: 1});");
            using var result = _connection.Query("MATCH (x:T) RETURN x.id;");
            using var summary = result.GetQuerySummary();
            // Times may be zero on extremely fast systems, just assert non-negative
            Assert.IsTrue(summary.CompilingTimeMs >= 0);
            Assert.IsTrue(summary.ExecutionTimeMs >= 0);
        }

        [TestMethod]
        public void ArrowSchema_TryGet_ShouldReturnSchemaOrFalse()
        {
            EnsureNativeLibraryAvailable();
            using var create = _connection!.Query("CREATE NODE TABLE T(id INT64, PRIMARY KEY(id));");
            using var insert = _connection.Query("CREATE (:T {id: 42});");
            using var result = _connection.Query("MATCH (x:T) RETURN x.id;");
            var ok = result.TryGetArrowSchema(out ArrowSchema schema);
            // We cannot guarantee Arrow support in all builds; ensure call is safe.
            if (ok)
            {
                // basic sanity: format pointer should not be zero if success
                Assert.AreNotEqual(IntPtr.Zero, schema.format);
            }
        }

        [TestMethod]
        public void ArrowChunk_TryGet_ShouldNotThrow()
        {
            EnsureNativeLibraryAvailable();
            using var create = _connection!.Query("CREATE NODE TABLE T(id INT64, PRIMARY KEY(id));");
            using var insert1 = _connection.Query("CREATE (:T {id: 1});");
            using var insert2 = _connection.Query("CREATE (:T {id: 2});");
            using var result = _connection.Query("MATCH (x:T) RETURN x.id ORDER BY x.id;");
            // Attempt to fetch a small chunk
            var ok = result.TryGetNextArrowChunk(10, out ArrowArray array);
            if (ok)
            {
                Assert.IsTrue(array.length >= 0);
            }
        }
    }
}