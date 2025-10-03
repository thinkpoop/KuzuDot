using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KuzuDot;

namespace KuzuDot.Tests.ResourceManagement
{
    /// <summary>
    /// Tests to detect memory leaks and verify proper native resource cleanup.
    /// These tests focus on explicit disposal without relying on finalizers.
    /// </summary>
    [TestClass]
    public class MemoryLeakTests
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
        public void RepetitiveConnectionCreation_WithProperDisposal_ShouldNotLeakMemory()
        {
            EnsureNativeLibraryAvailable();

            var initialMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Initial memory: {initialMemory:N0} bytes");

            const int iterations = 100;
            
            for (int i = 0; i < iterations; i++)
            {
                using var connection = _database!.Connect();
                // Connection is automatically disposed by using statement
                
                // Force cleanup every 10 iterations
                if (i % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    var currentMemory = GC.GetTotalMemory(false);
                    Console.WriteLine($"Memory after {i + 1} iterations: {currentMemory:N0} bytes");
                }
            }

            // Final memory check
            var finalMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Final memory: {finalMemory:N0} bytes");
            var memoryGrowth = finalMemory - initialMemory;
            Console.WriteLine($"Memory growth: {memoryGrowth:N0} bytes");

            // Memory should not grow excessively (allowing for reasonable overhead)
            Assert.IsTrue(memoryGrowth < 1_000_000, $"Memory grew by {memoryGrowth:N0} bytes, indicating potential leak");
        }

        [TestMethod]
        public void RepetitiveQueryExecution_WithProperDisposal_ShouldNotLeakMemory()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            
            // Setup schema
            using (var setupResult = connection.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name));"))
            {
                Assert.IsTrue(setupResult.IsSuccess);
            }

            var initialMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Initial memory: {initialMemory:N0} bytes");

            const int iterations = 50;
            
            for (int i = 0; i < iterations; i++)
            {
                string query = $"CREATE (:Person {{name: 'Person{i}', age: {20 + (i % 50)}}});";
                
                using var result = connection.Query(query);
                Assert.IsTrue(result.IsSuccess);
                // QueryResult is automatically disposed by using statement
                
                // Force cleanup every 5 iterations
                if (i % 5 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    var currentMemory = GC.GetTotalMemory(false);
                    Console.WriteLine($"Memory after {i + 1} queries: {currentMemory:N0} bytes");
                }
            }

            // Final memory check
            var finalMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Final memory: {finalMemory:N0} bytes");
            var memoryGrowth = finalMemory - initialMemory;
            Console.WriteLine($"Memory growth: {memoryGrowth:N0} bytes");

            // Memory should not grow excessively (allowing for reasonable overhead)
            Assert.IsTrue(memoryGrowth < 2_000_000, $"Memory grew by {memoryGrowth:N0} bytes, indicating potential leak");
        }

        [TestMethod]
        public void RepetitivePreparedStatementExecution_WithProperDisposal_ShouldNotLeakMemory()
        {
            EnsureNativeLibraryAvailable();

            using var connection = _database!.Connect();
            
            // Setup schema
            using (var setupResult = connection.Query("CREATE NODE TABLE TestNode(id INT64, value STRING, PRIMARY KEY(id));"))
            {
                Assert.IsTrue(setupResult.IsSuccess);
            }

            var initialMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Initial memory: {initialMemory:N0} bytes");

            const int iterations = 50;
            
            for (int i = 0; i < iterations; i++)
            {
                using var statement = connection.Prepare("CREATE (:TestNode {id: $id, value: $value});");
                
                statement.BindInt64("id", i);
                statement.BindString("value", $"TestValue{i}");
                
                using var result = statement.Execute();
                Assert.IsTrue(result.IsSuccess);
                // Both PreparedStatement and QueryResult are automatically disposed
                
                // Force cleanup every 5 iterations
                if (i % 5 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    var currentMemory = GC.GetTotalMemory(false);
                    Console.WriteLine($"Memory after {i + 1} statements: {currentMemory:N0} bytes");
                }
            }

            // Final memory check
            var finalMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Final memory: {finalMemory:N0} bytes");
            var memoryGrowth = finalMemory - initialMemory;
            Console.WriteLine($"Memory growth: {memoryGrowth:N0} bytes");

            // Memory should not grow excessively (allowing for reasonable overhead)
            Assert.IsTrue(memoryGrowth < 2_000_000, $"Memory grew by {memoryGrowth:N0} bytes, indicating potential leak");
        }

        [TestMethod]
        public void KuzuValueCreation_WithProperDisposal_ShouldNotLeakMemory()
        {
            EnsureNativeLibraryAvailable();

            var initialMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Initial memory: {initialMemory:N0} bytes");

            const int iterations = 1000;
            
            for (int i = 0; i < iterations; i++)
            {
                // Test various KuzuValue types (only those supported by the API)
                using (var stringValue = KuzuValue.CreateString($"TestString{i}"))
                using (var intValue = KuzuValue.CreateInt64(i))
                using (var boolValue = KuzuValue.CreateBool(i % 2 == 0))
                using (var nullValue = KuzuValue.CreateNull())
                {
                    // Verify values work correctly
                    Assert.AreEqual($"TestString{i}", stringValue.GetString());
                    Assert.AreEqual(i, intValue.GetInt64());
                    Assert.AreEqual(i % 2 == 0, boolValue.GetBool());
                    Assert.IsTrue(nullValue.IsNull());
                    
                    // All values automatically disposed by using statement
                }
                
                // Force cleanup every 100 iterations
                if (i % 100 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    var currentMemory = GC.GetTotalMemory(false);
                    Console.WriteLine($"Memory after {i + 1} value creations: {currentMemory:N0} bytes");
                }
            }

            // Final memory check
            var finalMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Final memory: {finalMemory:N0} bytes");
            var memoryGrowth = finalMemory - initialMemory;
            Console.WriteLine($"Memory growth: {memoryGrowth:N0} bytes");

            // Memory should not grow excessively (allowing for reasonable overhead)
            Assert.IsTrue(memoryGrowth < 1_000_000, $"Memory grew by {memoryGrowth:N0} bytes, indicating potential leak");
        }

        [TestMethod]
        public void NestedResourceUsage_WithProperDisposal_ShouldNotLeakMemory()
        {
            EnsureNativeLibraryAvailable();

            var initialMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Initial memory: {initialMemory:N0} bytes");

            const int iterations = 20;
            
            for (int i = 0; i < iterations; i++)
            {
                using var tempDb = new Database(":memory:");
                using var connection = tempDb.Connect();
                
                // Create schema
                using (var schemaResult = connection.Query("CREATE NODE TABLE TempNode(id INT64, PRIMARY KEY(id));"))
                {
                    Assert.IsTrue(schemaResult.IsSuccess);
                }
                
                // Execute multiple operations with nested resource usage
                for (int j = 0; j < 10; j++)
                {
                    using var statement = connection.Prepare("CREATE (:TempNode {id: $id});");
                    statement.BindInt64("id", j);
                    
                    using var result = statement.Execute();
                    Assert.IsTrue(result.IsSuccess);
                }
                
                // Query back the data count (without accessing individual rows)
                using (var queryResult = connection.Query("MATCH (n:TempNode) RETURN COUNT(n);"))
                {
                    Assert.IsTrue(queryResult.IsSuccess);
                    Assert.AreEqual(1UL, queryResult.NumColumns);
                }
                
                // All resources automatically disposed at end of using blocks
                
                // Force cleanup every 5 iterations
                if (i % 5 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    var currentMemory = GC.GetTotalMemory(false);
                    Console.WriteLine($"Memory after {i + 1} nested operations: {currentMemory:N0} bytes");
                }
            }

            // Final memory check
            var finalMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Final memory: {finalMemory:N0} bytes");
            var memoryGrowth = finalMemory - initialMemory;
            Console.WriteLine($"Memory growth: {memoryGrowth:N0} bytes");

            // Memory should not grow excessively (allowing for reasonable overhead)
            Assert.IsTrue(memoryGrowth < 3_000_000, $"Memory grew by {memoryGrowth:N0} bytes, indicating potential leak");
        }
    }
}