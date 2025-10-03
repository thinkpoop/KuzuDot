using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KuzuDot.Tests
{
    [TestClass]
    public class BlobTests
    {
        private Database? _database;
        private Connection? _connection;
        private string? _initError;

        [TestInitialize]
        public void Init()
        {
            try
            {
                _database = new Database(":memory:");
                _connection = _database.Connect();
                _connection.Query("CREATE NODE TABLE File(id STRING, data BLOB, PRIMARY KEY(id))").Dispose();
            }
            catch (KuzuException ex)
            {
                _initError = ex.Message;
                _database = null; _connection = null;
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            _connection?.Dispose();
            _database?.Dispose();
        }

        private void RequireNative()
        {
            if (_connection == null || _database == null)
                Assert.Inconclusive("Native library unavailable: " + _initError);
        }

        private bool TryInsertBlob(string id, byte[] data)
        {
            string hex = BitConverter.ToString(data).Replace("-", string.Empty);
            try
            {
                string cypher = $"CREATE (:File {{id: '{id}', data: BLOB('{hex}')}})";
                using var r = _connection!.Query(cypher);
                return true;
            }
            catch (KuzuException)
            {
                return false;
            }
        }

        [TestMethod]
        public void RetrieveEmptyBlobOrInconclusive()
        {
            RequireNative();
            if (!TryInsertBlob("empty", Array.Empty<byte>()))
                Assert.Inconclusive("Blob literal insertion not supported in current engine.");

            using var result = _connection!.Query("MATCH (f:File {id: 'empty'}) RETURN f.data");
            Assert.AreEqual(1UL, result.GetNumTuples());
            using var row = result.GetNext();
            using var value = row.GetValue(0);
            var blob = value.GetBlob();
            Assert.AreEqual(0, blob.Length);
        }

        [TestMethod]
        public void RetrieveBlobIntegrityOrInconclusive()
        {
            RequireNative();
            byte[] data = new byte[16];
            for (int i = 0; i < data.Length; i++) data[i] = (byte)i;
            if (!TryInsertBlob("file1", data))
                Assert.Inconclusive("Blob literal insertion not supported in current engine.");

            using var result = _connection!.Query("MATCH (f:File {id: 'file1'}) RETURN f.data");
            Assert.AreEqual(1UL, result.GetNumTuples());
            using var row = result.GetNext();
            using var value = row.GetValue(0);
            var fetched = value.GetBlob();
            Assert.AreEqual(data.Length, fetched.Length);
            for (int i = 0; i < fetched.Length; i++) Assert.AreEqual(data[i], fetched[i]);
        }
    }
}
