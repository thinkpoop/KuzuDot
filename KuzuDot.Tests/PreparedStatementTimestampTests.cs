using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KuzuDot;

namespace KuzuDot.Tests
{
    [TestClass]
    public class PreparedStatementTimestampTests
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
                Assert.Inconclusive($"Native library unavailable: {_initializationError}");
            }
        }

        private static long NowMicro() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;

        [TestMethod]
        public void BindTimestampVariants_ShouldSucceed()
        {
            EnsureNativeLibraryAvailable();

            using var create = _connection!.Query(@"CREATE NODE TABLE EVT(
                id INT64,
                ts_base TIMESTAMP,
                ts_ns TIMESTAMP_NS,
                ts_ms TIMESTAMP_MS,
                ts_sec TIMESTAMP_SEC,
                ts_tz TIMESTAMP_TZ,
                PRIMARY KEY(id)
            )");

            long baseMicros = NowMicro();
            long ns = baseMicros * 1000; // micros -> nanos
            long ms = baseMicros / 1000; // micros -> millis
            long sec = baseMicros / 1_000_000; // micros -> seconds
            long tzMicros = baseMicros; // microseconds (assume UTC)
            var baseTs = DateTime.UtcNow;

            using var stmt = _connection.Prepare(@"CREATE (:EVT {id: $id, ts_base: $b, ts_ns: $n, ts_ms: $m, ts_sec: $s, ts_tz: $z})");
            Assert.IsTrue(stmt.IsSuccess, stmt.ErrorMessage);

            stmt.BindInt64("id", 1);
            stmt.BindTimestamp("b", baseTs);
            stmt.BindTimestampNanoseconds("n", ns);
            stmt.BindTimestampMilliseconds("m", ms);
            stmt.BindTimestampSeconds("s", sec);
            stmt.BindTimestampWithTimeZone("z", DateTimeOffset.FromUnixTimeMilliseconds(tzMicros / 1000));

            using var insertRes = stmt.Execute();
            Assert.IsTrue(insertRes.IsSuccess);
        }
    }
}
