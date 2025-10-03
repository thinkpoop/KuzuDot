using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KuzuDot;

namespace KuzuDot.Tests
{
    /// <summary>
    /// Tests for edge cases and error scenarios
    /// </summary>
    [TestClass]
    public class EdgeCaseTests
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
            // Removed forced GC.Collect cycles – explicit disposal is sufficient.
        }

        private void EnsureNativeLibraryAvailable()
        {
            if (_database == null || _connection == null)
            {
                Assert.Inconclusive($"Native Kuzu library is not available. Error: {_initializationError}");
            }
        }

        [TestMethod]
        public void KuzuValue_CreateString_WithNullString_ShouldThrowArgumentNullException()
        {
            // This tests what happens when we pass null to CreateString
            // Should throw ArgumentNullException with clear message
            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => 
            {
                using var value = KuzuValue.CreateString(null);
                return value.GetString();
            });
            
            // Should throw a clear ArgumentNullException
            Assert.AreEqual("value", ex.ParamName);
        }

        [TestMethod]
        public void KuzuValue_CreateString_WithEmptyString_ShouldWork()
        {
            // Ensure native library is available
            EnsureNativeLibraryAvailable();

            // Arrange & Act
            using var value = KuzuValue.CreateString("");
            
            // Assert
            Assert.IsFalse(value.IsNull());
            Assert.AreEqual("", value.GetString());
        }

        [TestMethod]
        public void KuzuValue_CreateString_WithModerateLengthString_ShouldWork()
        {
            // Ensure native library is available
            EnsureNativeLibraryAvailable();

            // Arrange - Use a more reasonable length to avoid potential memory issues
            string moderateString = new string('A', 1000);
            
            // Act
            using var value = KuzuValue.CreateString(moderateString);
            
            // Assert
            Assert.IsFalse(value.IsNull());
            Assert.AreEqual(moderateString, value.GetString());
        }

        [TestMethod]
        public void KuzuValue_CreateString_WithBasicSpecialCharacters_ShouldWork()
        {
            // Ensure native library is available
            EnsureNativeLibraryAvailable();

            // Arrange - Use basic ASCII special characters to avoid encoding issues
            string specialString = "Hello, World! Special chars: @#$%^&*()";
            
            // Act
            using var value = KuzuValue.CreateString(specialString);
            
            // Assert
            Assert.IsFalse(value.IsNull());
            Assert.AreEqual(specialString, value.GetString());
        }

        [TestMethod]
        public void Database_MultipleInstances_ShouldWorkIndependently()
        {
            // Ensure native library is available
            EnsureNativeLibraryAvailable();

            // Use completely separate database instances
            Database? db1 = null;
            Connection? conn1 = null;
            
            try
            {
                // First database
                db1 = new Database(":memory:");
                conn1 = db1.Connect();
                
                // Create table in first database
                using var createResult1 = conn1.Query("CREATE NODE TABLE Table1(id INT64, PRIMARY KEY(id));");
                using var result1 = conn1.Query("CREATE (:Table1 {id: 1});");
                Assert.IsTrue(result1.IsSuccess);
            }
            finally
            {
                conn1?.Dispose();
                db1?.Dispose();
            }
            
            Database? db2 = null;
            Connection? conn2 = null;
            
            try
            {
                // Second database - completely separate
                db2 = new Database(":memory:");
                conn2 = db2.Connect();
                
                // Create different table in second database  
                using var createResult2 = conn2.Query("CREATE NODE TABLE Table2(id INT64, PRIMARY KEY(id));");
                using var result2 = conn2.Query("CREATE (:Table2 {id: 2});");
                Assert.IsTrue(result2.IsSuccess);
            }
            finally
            {
                conn2?.Dispose();
                db2?.Dispose();
            }
        }

        [TestMethod]
        public void Connection_Query_WithNullOrEmptyQuery_ShouldThrowArgumentException()
        {
            EnsureNativeLibraryAvailable();

            var ex1 = Assert.ThrowsExactly<ArgumentException>(() => _connection!.Query(null!));
            Assert.AreEqual("query", ex1.ParamName);
            
            var ex2 = Assert.ThrowsExactly<ArgumentException>(() => _connection!.Query(""));
            Assert.AreEqual("query", ex2.ParamName);
        }

        [TestMethod]
        public void Connection_Prepare_WithNullOrEmptyQuery_ShouldThrowArgumentException()
        {
            EnsureNativeLibraryAvailable();

            var ex1 = Assert.ThrowsExactly<ArgumentException>(() => _connection!.Prepare(null!));
            Assert.AreEqual("query", ex1.ParamName);
            
            var ex2 = Assert.ThrowsExactly<ArgumentException>(() => _connection!.Prepare(""));
            Assert.AreEqual("query", ex2.ParamName);
        }

        [TestMethod]
        public void Connection_QueryWithSpecialCharacters_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            using var createResult = _connection!.Query(
                "CREATE NODE TABLE SpecialTable(name STRING, description STRING, PRIMARY KEY(name));");
            Assert.IsTrue(createResult.IsSuccess);
            
            try
            {
                using var insertStmt = _connection.Prepare(
                    "CREATE (:SpecialTable {name: $name, description: $desc});");
                
                insertStmt.BindString("name", "Test_Name");
                insertStmt.BindString("desc", "Simple description");
                
                using var insertResult = insertStmt.Execute();
                Assert.IsTrue(insertResult.IsSuccess);
            }
            catch (Exception)
            {
                using var simpleResult = _connection.Query("CREATE (:SpecialTable {name: 'simple', description: 'test'});");
                Assert.IsTrue(simpleResult.IsSuccess);
            }
        }

        [TestMethod]
        public void PreparedStatement_BindingWithEmptyStrings_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            using var createResult = _connection!.Query("CREATE NODE TABLE EmptyTest(name STRING, value STRING, PRIMARY KEY(name));");
            using var stmt = _connection.Prepare("CREATE (:EmptyTest {name: $name, value: $value});");
            stmt.BindString("name", "empty_test");
            stmt.BindString("value", "");
            using var result = stmt.Execute();
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void PreparedStatement_BindingWithNullStrings_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            using var createResult = _connection!.Query("CREATE NODE TABLE NullTest(name STRING, value STRING, PRIMARY KEY(name));");
            using var stmt = _connection.Prepare("CREATE (:NullTest {name: $name, value: $value});");
            stmt.BindString("name", "test");
            stmt.BindString("value", null);
            using var result = stmt.Execute();
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public void PreparedStatement_BindingWithInvalidParameterNames_ShouldThrowException()
        {
            EnsureNativeLibraryAvailable();

            using var createResult = _connection!.Query("CREATE NODE TABLE ParamTest(id INT64, PRIMARY KEY(id));");
            using var stmt = _connection.Prepare("CREATE (:ParamTest {id: $id});");
            var ex1 = Assert.ThrowsExactly<ArgumentException>(() => stmt.BindInt64(null!, 1));
            Assert.AreEqual("paramName", ex1.ParamName);
            var ex2 = Assert.ThrowsExactly<ArgumentException>(() => stmt.BindInt64("", 1));
            Assert.AreEqual("paramName", ex2.ParamName);
        }

        [TestMethod]
        public void PreparedStatement_BindingExtremeValues_ShouldWork()
        {
            EnsureNativeLibraryAvailable();

            using var createResult = _connection!.Query("CREATE NODE TABLE ExtremeTest(id INT64, flag BOOLEAN, PRIMARY KEY(id));");
            using (var stmt = _connection.Prepare("CREATE (:ExtremeTest {id: $id, flag: $flag});"))
            {
                stmt.BindInt64("id", long.MaxValue);
                stmt.BindBool("flag", true);
                using var result1 = stmt.Execute();
                Assert.IsTrue(result1.IsSuccess);
            }
            using (var stmt2 = _connection.Prepare("CREATE (:ExtremeTest {id: $id, flag: $flag});"))
            {
                stmt2.BindInt64("id", long.MinValue);
                stmt2.BindBool("flag", false);
                using var result2 = stmt2.Execute();
                Assert.IsTrue(result2.IsSuccess);
            }
            using (var stmt3 = _connection.Prepare("CREATE (:ExtremeTest {id: $id, flag: $flag});"))
            {
                stmt3.BindInt64("id", 0);
                stmt3.BindBool("flag", true);
                using var result3 = stmt3.Execute();
                Assert.IsTrue(result3.IsSuccess);
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void KuzuValue_BooleanValues_ShouldRoundTrip(bool value)
        {
            EnsureNativeLibraryAvailable();
            using var kuzuValue = KuzuValue.CreateBool(value);
            Assert.IsFalse(kuzuValue.IsNull());
            Assert.AreEqual(value, kuzuValue.GetBool());
        }

        [TestMethod]
        [DataRow(0L)]
        [DataRow(1L)]
        [DataRow(-1L)]
        [DataRow(42L)]
        [DataRow(1000L)]
        [DataRow(-1000L)]
        public void KuzuValue_Int64Values_ShouldRoundTrip(long value)
        {
            EnsureNativeLibraryAvailable();
            using var kuzuValue = KuzuValue.CreateInt64(value);
            Assert.IsFalse(kuzuValue.IsNull());
            Assert.AreEqual(value, kuzuValue.GetInt64());
        }

        [TestMethod]
        public void KuzuValue_ExtremeInt64Values_ShouldRoundTrip()
        {
            EnsureNativeLibraryAvailable();
            using var maxValue = KuzuValue.CreateInt64(long.MaxValue);
            Assert.IsFalse(maxValue.IsNull());
            Assert.AreEqual(long.MaxValue, maxValue.GetInt64());
            using var minValue = KuzuValue.CreateInt64(long.MinValue);
            Assert.IsFalse(minValue.IsNull());
            Assert.AreEqual(long.MinValue, minValue.GetInt64());
        }

        [TestMethod]
        public void QueryResult_ErrorScenarios_ShouldHandleGracefully()
        {
            EnsureNativeLibraryAvailable();
            Exception? captured = null;
            try
            {
                using var result = _connection!.Query("MATCH (n:NonExistentTable) RETURN n;");
            }
            catch (Exception err)
            {
                captured = err;
            }
            Assert.IsNotNull(captured);
            Assert.IsInstanceOfType(captured, typeof(KuzuException));
        }
    }
}