using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KuzuDot;

namespace KuzuDot.Tests.ResourceManagement
{
    /// <summary>
    /// Tests to verify proper resource management patterns and disposal order.
    /// Focuses on testing explicit disposal without relying on finalizers.
    /// </summary>
    [TestClass]
    public class ResourceManagementTests
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
                _database = null;
                _initializationError = ex.Message;
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _database?.Dispose();
            
            // Force cleanup to ensure test isolation
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

        [TestMethod]
        public void ExplicitDisposal_InCorrectOrder_ShouldNotThrow()
        {
            EnsureNativeLibraryAvailable();

            Database? database = null;
            Connection? connection = null;
            QueryResult? result = null;
            PreparedStatement? statement = null;

            try
            {
                // Create resources in order
                database = new Database(":memory:");
                connection = database.Connect();
                
                // Setup schema
                using (var setupResult = connection.Query("CREATE NODE TABLE TestNode(id INT64, PRIMARY KEY(id));"))
                {
                    Assert.IsTrue(setupResult.IsSuccess);
                }
                
                statement = connection.Prepare("CREATE (:TestNode {id: $id});");
                statement.BindInt64("id", 42);
                result = statement.Execute();
                
                Assert.IsTrue(result.IsSuccess);
                Console.WriteLine("? All resources created successfully");
            }
            finally
            {
                // Dispose in reverse order of creation (CRITICAL for native resources)
                Console.WriteLine("?? Disposing resources in reverse order...");
                
                result?.Dispose();
                Console.WriteLine("? QueryResult disposed");
                
                statement?.Dispose();
                Console.WriteLine("? PreparedStatement disposed");
                
                connection?.Dispose();
                Console.WriteLine("? Connection disposed");
                
                database?.Dispose();
                Console.WriteLine("? Database disposed");
            }
            
            Console.WriteLine("? Test completed without exceptions");
        }

        [TestMethod]
        public void ExplicitDisposal_InWrongOrder_ShouldStillWork()
        {
            EnsureNativeLibraryAvailable();

            Database? database = null;
            Connection? connection = null;
            QueryResult? result = null;

            try
            {
                database = new Database(":memory:");
                connection = database.Connect();
                result = connection.Query("CREATE NODE TABLE TestNode(id INT64, PRIMARY KEY(id));");
                
                Assert.IsTrue(result.IsSuccess);
                Console.WriteLine("? All resources created successfully");
            }
            finally
            {
                // Dispose in wrong order (database first) - should still work due to proper implementation
                Console.WriteLine("?? Disposing resources in wrong order (testing robustness)...");
                
                database?.Dispose();
                Console.WriteLine("? Database disposed first");
                
                connection?.Dispose();
                Console.WriteLine("? Connection disposed after database");
                
                result?.Dispose();
                Console.WriteLine("? QueryResult disposed last");
            }
            
            Console.WriteLine("? Test completed - implementation handles wrong disposal order gracefully");
        }

        [TestMethod]
        public void DoubleDispose_ShouldNotThrow()
        {
            EnsureNativeLibraryAvailable();

            var database = new Database(":memory:");
            var connection = database.Connect();
            
            // Setup schema
            using (var setupResult = connection.Query("CREATE NODE TABLE TestNode(id INT64, PRIMARY KEY(id));"))
            {
                Assert.IsTrue(setupResult.IsSuccess);
            }
            
            var statement = connection.Prepare("CREATE (:TestNode {id: $id});");
            statement.BindInt64("id", 123);
            var result = statement.Execute();
            
            Assert.IsTrue(result.IsSuccess);
            
            // First disposal
            Console.WriteLine("?? First disposal...");
            result.Dispose();
            statement.Dispose();
            connection.Dispose();
            database.Dispose();
            Console.WriteLine("? First disposal completed");
            
            // Second disposal should not throw
            Console.WriteLine("?? Second disposal (should be safe)...");
            result.Dispose();
            statement.Dispose();
            connection.Dispose();
            database.Dispose();
            Console.WriteLine("? Second disposal completed without exceptions");
        }

        [TestMethod]
        public void ResourcesAfterDispose_ShouldThrowObjectDisposedException()
        {
            EnsureNativeLibraryAvailable();

            var database = new Database(":memory:");
            var connection = database.Connect();
            
            // Dispose the database
            database.Dispose();
            
            // Using the disposed database should throw ObjectDisposedException
            Console.WriteLine("?? Testing database usage after disposal...");
            Assert.ThrowsExactly<ObjectDisposedException>(() => database.Connect());
            Console.WriteLine("? Database correctly throws ObjectDisposedException after disposal");
            
            // Dispose the connection (should handle gracefully even if parent is disposed)
            connection.Dispose();
            
            // Using the disposed connection should throw ObjectDisposedException
            Console.WriteLine("?? Testing connection usage after disposal...");
            Assert.ThrowsExactly<ObjectDisposedException>(() => connection.Query("SELECT 1;"));
            Console.WriteLine("? Connection correctly throws ObjectDisposedException after disposal");
        }

        [TestMethod]
        public void ExceptionDuringResourceUsage_ShouldNotPreventProperCleanup()
        {
            EnsureNativeLibraryAvailable();

            Database? database = null;
            Connection? connection = null;
            QueryResult? result = null;
            bool exceptionThrown = false;

            try
            {
                database = new Database(":memory:");
                connection = database.Connect();
                
                // This should throw an exception (invalid SQL)
                result = connection.Query("INVALID SQL THAT WILL THROW EXCEPTION");
                
                // Should not reach here
                Assert.IsTrue(false, "Expected exception was not thrown");
            }
            catch (AssertFailedException)
            {
                // Re-throw Assert exceptions
                throw;
            }
            catch (Exception ex)
            {
                exceptionThrown = true;
                Console.WriteLine($"? Expected exception caught: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                // Cleanup should happen even after exception
                Console.WriteLine("?? Cleaning up resources after exception...");
                
                result?.Dispose();
                Console.WriteLine("? QueryResult disposed (was null due to exception)");
                
                connection?.Dispose();
                Console.WriteLine("? Connection disposed");
                
                database?.Dispose();
                Console.WriteLine("? Database disposed");
            }
            
            Assert.IsTrue(exceptionThrown, "Exception should have been thrown and caught");
            Console.WriteLine("? Test completed - resources cleaned up properly despite exception");
        }

        [TestMethod]
        public void MultipleConnections_FromSameDatabase_ShouldManageIndependently()
        {
            EnsureNativeLibraryAvailable();

            using var database = _database!;
            
            Connection? connection1 = null;
            Connection? connection2 = null;
            
            try
            {
                connection1 = database.Connect();
                connection2 = database.Connect();
                
                // Setup schema on first connection
                using (var setupResult = connection1.Query("CREATE NODE TABLE MultiConnTest(id INT64, PRIMARY KEY(id));"))
                {
                    Assert.IsTrue(setupResult.IsSuccess);
                }
                
                // Use both connections
                using (var result1 = connection1.Query("CREATE (:MultiConnTest {id: 1});"))
                using (var result2 = connection2.Query("CREATE (:MultiConnTest {id: 2});"))
                {
                    Assert.IsTrue(result1.IsSuccess);
                    Assert.IsTrue(result2.IsSuccess);
                    Console.WriteLine("? Both connections work independently");
                }
                
                // Dispose one connection
                connection1.Dispose();
                connection1 = null;
                Console.WriteLine("? First connection disposed");
                
                // Second connection should still work
                using (var result = connection2.Query("MATCH (n:MultiConnTest) RETURN count(n);"))
                {
                    Assert.IsTrue(result.IsSuccess);
                    Console.WriteLine("? Second connection still works after first was disposed");
                }
            }
            finally
            {
                connection1?.Dispose();
                connection2?.Dispose();
                Console.WriteLine("? All connections cleaned up");
            }
        }

        [TestMethod]
        public void LargeResultSet_WithProperDisposal_ShouldNotLeakMemory()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            
            // Setup schema
            using (var setupResult = connection.Query("CREATE NODE TABLE LargeTest(id INT64, data STRING, PRIMARY KEY(id));"))
            {
                Assert.IsTrue(setupResult.IsSuccess);
            }
            
            var initialMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Initial memory: {initialMemory:N0} bytes");
            
            // Insert a moderate amount of data
            const int recordCount = 100;
            using var statement = connection.Prepare("CREATE (:LargeTest {id: $id, data: $data});");
            
            for (int i = 0; i < recordCount; i++)
            {
                statement.BindInt64("id", i);
                statement.BindString("data", $"Large data string for record {i} - " + new string('X', 100));
                
                using var insertResult = statement.Execute();
                Assert.IsTrue(insertResult.IsSuccess);
            }
            
            Console.WriteLine($"? Inserted {recordCount} records");
            
            // Query all data back and verify proper disposal (without accessing individual rows)
            using (var queryResult = connection.Query("MATCH (n:LargeTest) RETURN n.id, n.data ORDER BY n.id;"))
            {
                Assert.IsTrue(queryResult.IsSuccess);
                Assert.AreEqual(2UL, queryResult.NumColumns); // Should have 2 columns: id and data
                Assert.IsTrue(queryResult.HasNext()); // Should have at least one result
                
                Console.WriteLine($"? Retrieved query result with {queryResult.NumColumns} columns and confirmed data exists");
            }
            
            // Final memory check
            var finalMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Final memory: {finalMemory:N0} bytes");
            var memoryGrowth = finalMemory - initialMemory;
            Console.WriteLine($"Memory growth: {memoryGrowth:N0} bytes");
            
            // Memory should not grow excessively
            Assert.IsTrue(memoryGrowth < 5_000_000, $"Memory grew by {memoryGrowth:N0} bytes, indicating potential leak");
        }
    }
}