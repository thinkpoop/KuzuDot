using System;
using System.Collections.Generic;
using KuzuDot.Native;

namespace KuzuDot.Examples
{
    /// <summary>
    /// Comprehensive example demonstrating key KuzuDot functionality with
    /// structured console output and simple aggregate calculations.
    /// </summary>
    public class ComprehensiveExample
    {
        public static void Run()
        {
            ExampleLog.Title("KuzuDot Comprehensive Example");
            ExampleLog.SafeStep("Display Version Info", DisplayVersionInfo);

            using var database = ExampleLog.SafeCreate("Create In-Memory Database", () => new Database(":memory:"));
            using var connection = ExampleLog.SafeCreate("Create Connection", () => database.Connect());

            ExampleLog.SafeStep("Configure Connection", () => ConfigureConnection(connection));
            ExampleLog.SafeStep("Create Schema", () => CreateTestSchema(connection));
            ExampleLog.SafeStep("Insert & Retrieve Data", () => DemonstrateDataTypes(connection));

            ExampleLog.Success("Example completed successfully");
        }

        // ------------------ Core Steps ------------------
        private static void DisplayVersionInfo()
        {
            ExampleLog.Info($"Kuzu Version        : {Version.GetVersion()}");
            ExampleLog.Info($"Storage Version     : {Version.GetStorageVersion()}");
        }

        private static void ConfigureConnection(Connection connection)
        {
            ExampleLog.Info($"Original Threads    : {connection.MaxNumThreadsForExecution}");
            connection.MaxNumThreadsForExecution = 4;
            connection.SetQueryTimeout(30000); // 30 seconds
            ExampleLog.Info($"Updated Threads     : {connection.MaxNumThreadsForExecution}");
        }

        private static void CreateTestSchema(Connection connection)
        {
            using var result = connection.Query(@"
                CREATE NODE TABLE Person(
                    id INT64,
                    name STRING,
                    age INT32,
                    height FLOAT,
                    weight DOUBLE,
                    is_active BOOLEAN,
                    birth_date DATE,
                    created_at TIMESTAMP,
                    PRIMARY KEY(id)
                )");
            Ensure(result.IsSuccess, () => $"Failed to create schema: {result.GetErrorMessage()}");
            ExampleLog.Success("Schema created");
        }

        private static void DemonstrateDataTypes(Connection connection)
        {
            ExampleLog.Section("Data Insertion & Retrieval");

            var insertQuery = @"CREATE (p:Person {
                id: $id,
                name: $name,
                age: $age,
                height: $height,
                weight: $weight,
                is_active: $is_active,
                birth_date: $birth_date,
                created_at: $created_at
            })";

            using var stmt = connection.Prepare(insertQuery);
            Ensure(stmt.IsSuccess, () => $"Prepare failed: {stmt.ErrorMessage}");

            InsertPersonRecord(stmt, 1, "Alice Johnson", 30, 5.6f, 140.5, true, new DateTime(1994, 3, 15), DateTime.UtcNow);
            InsertPersonRecord(stmt, 2, "Bob Smith", 25, 6.0f, 180.0, false, new DateTime(1999, 7, 22), DateTime.UtcNow);
            InsertPersonRecord(stmt, 3, "Charlie King", 42, 5.9f, 205.2, true, new DateTime(1982, 12, 3), DateTime.UtcNow);

            RetrieveAndCalculate(connection);
        }

        private static void InsertPersonRecord(PreparedStatement stmt, long id, string name,
            int age, float height, double weight, bool isActive, DateTime birthDate, DateTime createdAt)
        {
            stmt.Bind("id", id);
            stmt.Bind("name", name);
            stmt.Bind("age", age);
            stmt.Bind("height", height);
            stmt.Bind("weight", weight);
            stmt.Bind("is_active", isActive);
            stmt.BindDate("birth_date", birthDate.Date); // DATE binding
            stmt.BindTimestamp("created_at", createdAt); // TIMESTAMP binding

            using var result = stmt.Execute();
            Ensure(result.IsSuccess, () => $"Insert failed for '{name}': {result.GetErrorMessage()}");
            ExampleLog.Success($"Inserted person: {name} (ID={id})");
        }

        private static void RetrieveAndCalculate(Connection connection)
        {
            ExampleLog.Section("Row Retrieval");
            using var result = connection.Query("MATCH (p:Person) RETURN p.* ORDER BY p.id");
            Ensure(result.IsSuccess, () => $"Query failed: {result.GetErrorMessage()}");

            var count = result.GetNumTuples();
            ExampleLog.Info($"Rows returned       : {count}");

            // Aggregation variables
            long totalAge = 0;
            double totalWeight = 0.0;
            double totalHeightFeet = 0.0;
            int activeCount = 0;
            var bmiList = new List<double>();

            while (result.HasNext())
            {
                using var tuple = result.GetNext();
                using var idValue = tuple.GetValue(0);
                using var nameValue = tuple.GetValue(1);
                using var ageValue = tuple.GetValue(2);
                using var heightValue = tuple.GetValue(3);   // feet
                using var weightValue = tuple.GetValue(4);   // pounds
                using var isActiveValue = tuple.GetValue(5);
                using var birthDateValue = tuple.GetValue(6);
                using var createdAtValue = tuple.GetValue(7);

                var id = idValue.GetInt64();
                var name = nameValue.GetString();
                var age = ageValue.GetInt32();
                var heightFt = heightValue.GetFloat();
                var weightLbs = weightValue.GetDouble();
                var active = isActiveValue.GetBool();
                var birthDateStr = birthDateValue.GetDateAsString();
                var createdMicros = createdAtValue.GetTimestampUnixMicros();

                // Compute BMI (weight(lb) -> kg, height(ft) -> m)
                double heightMeters = heightFt * 0.3048;
                double weightKg = weightLbs * 0.45359237;
                double bmi = weightKg / (heightMeters * heightMeters);
                bmiList.Add(bmi);

                totalAge += age;
                totalWeight += weightLbs;
                totalHeightFeet += heightFt;
                if (active) activeCount++;

                Console.WriteLine($"  [ROW] ID={id} Name='{name}' Age={age} Active={active} Height={heightFt:F2}ft Weight={weightLbs:F1}lb BMI={bmi:F1} Birth={birthDateStr} CreatedMicros={createdMicros}");
            }

            ExampleLog.Section("Aggregates");
            if (count > 0)
            {
                double avgAge = totalAge / (double)count;
                double avgWeight = totalWeight / count;
                double avgHeight = totalHeightFeet / count;
                double avgBmi = bmiList.Count > 0 ? Sum(bmiList) / bmiList.Count : 0;
                int inactive = (int)count - activeCount;

                ExampleLog.Success($"Average Age        : {avgAge:F2}");
                ExampleLog.Success($"Average Height (ft): {avgHeight:F2}");
                ExampleLog.Success($"Average Weight (lb): {avgWeight:F2}");
                ExampleLog.Success($"Average BMI        : {avgBmi:F1}");
                ExampleLog.Info($"Active / Inactive  : {activeCount} / {inactive}");
            }
            else
            {
                ExampleLog.Info("No rows to aggregate");
            }
        }

        private static double Sum(List<double> list)
        {
            double s = 0; for (int i = 0; i < list.Count; i++) s += list[i]; return s;
        }

        // ------------------ Helpers ------------------
        private static void Ensure(bool condition, Func<string> errorFactory)
        {
            if (!condition) throw new Exception(errorFactory());
        }

        // (Optional extra demo retained but not invoked)
        private static void DemonstrateKuzuValueOperations()
        {
            ExampleLog.Section("Raw KuzuValue Operations");
            using (var nullValue = KuzuValue.CreateNull())
            using (var boolValue = KuzuValue.CreateBool(true))
            using (var int64Value = KuzuValue.CreateInt64(9223372036854775807L))
            using (var floatValue = KuzuValue.CreateFloat(3.14159f))
            using (var stringValue = KuzuValue.CreateString("Hello, KuzuDot!"))
            {
                ExampleLog.Info($"Null value IsNull  : {nullValue.IsNull()}");
                ExampleLog.Info($"Bool value         : {boolValue.GetBool()}");
                ExampleLog.Info($"Int64 value        : {int64Value.GetInt64()}");
                ExampleLog.Info($"Float value        : {floatValue.GetFloat()}");
                ExampleLog.Info($"String value       : '{stringValue.GetString()}'");

                using var clonedString = stringValue.Clone();
                ExampleLog.Info($"Cloned string      : '{clonedString.GetString()}'");

                stringValue.SetNull(true); ExampleLog.Info($"String SetNull(true) -> IsNull={stringValue.IsNull()}");
                stringValue.SetNull(false); ExampleLog.Info($"String SetNull(false)-> IsNull={stringValue.IsNull()}");
            }
            try
            {
                using var dateValue = KuzuValue.CreateDateFromString("2024-07-04");
                ExampleLog.Success($"Parsed date        : {dateValue.GetDateAsString()}");
            }
            catch (Exception ex)
            {
                ExampleLog.Error($"Date parsing failed: {ex.Message}");
            }
        }
    }
}