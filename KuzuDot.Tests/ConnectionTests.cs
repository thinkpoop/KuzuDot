using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KuzuDot;

namespace KuzuDot.Tests
{
    /// <summary>
    /// Comprehensive tests for Connection functionality including basic operations, 
    /// configuration, prepared statements, and resource management.
    /// </summary>
    [TestClass]
    public class ConnectionTests
    {
        private Database? _database;
        private string? _initializationError;

        [TestInitialize]
        public void TestInitialize()
        {
            try
            {
                _database = new Database(":memory:");
                _initializationError = null;
            }
            catch (KuzuException ex)
            {
                // Native library not available in test environment
                _database = null;
                _initializationError = ex.Message;
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _database?.Dispose();
            
            // Force garbage collection to ensure native resources are cleaned up
            // This is critical for test isolation when running in parallel
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void EnsureNativeLibraryAvailable()
        {
            if (_database == null)
            {
                throw new InvalidOperationException($"Cannot run test: Native Kuzu library is not available. Error: {_initializationError}");
            }
        }

        #region Basic Query Operations

        [TestMethod]
        public void Query_WithValidQuery_ShouldReturnQueryResult()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            string query = "CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));";
            
            using var result = connection.Query(query);
            
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void Query_WithInvalidQuery_ShouldThrowException()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            string invalidQuery = "INVALID SQL QUERY";
            
            Assert.ThrowsExactly<KuzuException>(() => connection.Query(invalidQuery));
        }

        [TestMethod]
        public void Query_AfterDispose_ShouldThrowObjectDisposedException()
        {
            EnsureNativeLibraryAvailable();

            var connection = _database!.Connect();
            connection.Dispose();
            
            Assert.ThrowsExactly<ObjectDisposedException>(() => connection.Query("SELECT 1;"));
        }

        #endregion

        #region Prepared Statement Operations

        [TestMethod]
        public void Prepare_WithValidQuery_ShouldReturnPreparedStatement()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            
            // First create a table
            using var createResult = connection.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));");
            
            string query = "CREATE (:Person {name: $name, age: $age});";
            
            using var preparedStatement = connection.Prepare(query);
            
            Assert.IsNotNull(preparedStatement);
            Assert.IsTrue(preparedStatement.IsSuccess);
        }

        [TestMethod]
        public void Prepare_WithInvalidQuery_ShouldReturnFailedPreparedStatement()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            string invalidQuery = "INVALID PREPARED QUERY";
            
            using var preparedStatement = connection.Prepare(invalidQuery);
            
            // The prepare method might not throw but return a failed prepared statement
            Assert.IsNotNull(preparedStatement);
            Assert.IsFalse(preparedStatement.IsSuccess);
            Assert.IsFalse(string.IsNullOrEmpty(preparedStatement.ErrorMessage));
        }

        [TestMethod]
        public void Prepare_AfterDispose_ShouldThrowObjectDisposedException()
        {
            EnsureNativeLibraryAvailable();

            var connection = _database!.Connect();
            connection.Dispose();
            
            Assert.ThrowsExactly<ObjectDisposedException>(() => connection.Prepare("SELECT 1;"));
        }

        [TestMethod]
        public void Execute_WithValidPreparedStatement_ShouldReturnQueryResult()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            
            // Create table
            using var createResult = connection.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));");
            
            using var preparedStatement = connection.Prepare("CREATE (:Person {name: $name, age: $age});");
            preparedStatement.BindString("name", "Alice");
            preparedStatement.BindInt64("age", 30);
            
            using var result = preparedStatement.Execute();
            
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
        }

        #endregion

        #region Connection Configuration

        [TestMethod]
        public void MaxNumThreadsForExecution_GetAndSet_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            
            // Get initial value
            var initialThreads = connection.MaxNumThreadsForExecution;
            Assert.IsTrue(initialThreads > 0, "Initial thread count should be positive");

            // Set new value
            var newThreadCount = initialThreads == 1 ? 2UL : 1UL;
            connection.MaxNumThreadsForExecution = newThreadCount;

            // Verify it was set
            Assert.AreEqual(newThreadCount, connection.MaxNumThreadsForExecution);

            // Set back to original
            connection.MaxNumThreadsForExecution = initialThreads;
            Assert.AreEqual(initialThreads, connection.MaxNumThreadsForExecution);
        }

        [TestMethod]
        public void SetQueryTimeout_ShouldNotThrow()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            
            // Test setting various timeout values
            connection.SetQueryTimeout(1000); // 1 second
            connection.SetQueryTimeout(5000); // 5 seconds
            connection.SetQueryTimeout(0);    // No timeout
        }

        [TestMethod]
        public void Interrupt_ShouldNotThrow()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            
            // Test that interrupt doesn't throw even when no query is running
            connection.Interrupt();
        }

        [TestMethod]
        public void MaxNumThreadsForExecution_WithZero_ShouldHandleGracefully()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            
            // Setting thread count to 0 should either throw or be handled gracefully
            try
            {
                connection.MaxNumThreadsForExecution = 0;
                // If it doesn't throw, verify that it's been set to some valid value
                Assert.IsTrue(connection.MaxNumThreadsForExecution >= 1);
            }
            catch (KuzuException)
            {
                // It's also valid for this to throw an exception
            }
        }

        [TestMethod]
        public void MaxNumThreadsForExecution_WithVeryLargeValue_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            var originalThreads = connection.MaxNumThreadsForExecution;

            try
            {
                // Try setting a very large value
                connection.MaxNumThreadsForExecution = 1000;
                
                // The implementation might cap this to a reasonable value
                var actualThreads = connection.MaxNumThreadsForExecution;
                Assert.IsTrue(actualThreads > 0);
                Assert.IsTrue(actualThreads <= 1000);
            }
            finally
            {
                // Restore original value
                connection.MaxNumThreadsForExecution = originalThreads;
            }
        }

        #endregion

        #region Resource Management

        [TestMethod]
        public void Dispose_ShouldNotThrow()
        {
            EnsureNativeLibraryAvailable();

            var connection = _database!.Connect();
            
            connection.Dispose(); // Should not throw
            connection.Dispose(); // Second call should also not throw
        }

        [TestMethod]
        public async Task Interrupt_DuringQuery_ShouldNotCrash()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            
            // Create a simple table for testing
            using (var createResult = connection.Query("CREATE NODE TABLE TestNode(id INT64, PRIMARY KEY(id))"))
            {
                Assert.IsTrue(createResult.IsSuccess);
            }

            // Insert some data
            using (var insertResult = connection.Query("CREATE (n:TestNode {id: 1})"))
            {
                Assert.IsTrue(insertResult.IsSuccess);
            }

            // Test that interrupt doesn't crash the connection
            var interruptTask = Task.Run(async () =>
            {
                await Task.Delay(50); // Wait a bit, then interrupt
                connection.Interrupt();
            });

            try
            {
                // Run a simple query (in a real scenario, this would be a long-running query)
                using (var queryResult = connection.Query("MATCH (n:TestNode) RETURN n.id"))
                {
                    // The query might succeed or be interrupted, both are valid
                }
            }
            catch (KuzuException)
            {
                // Interruption might cause an exception, which is expected
            }

            await interruptTask;
        }

        #endregion
    }
}