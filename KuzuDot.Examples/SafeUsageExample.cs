using System;
using KuzuDot;

namespace KuzuDot.Examples
{
    /// <summary>
    /// Example demonstrating safe usage patterns for KuzuDot.
    /// This example shows proper resource management and exception safety through explicit disposal.
    /// IMPORTANT: Always use 'using' statements or explicitly call Dispose() to ensure native resources are freed.
    /// </summary>
    public static class SafeUsageExample
    {
        /// <summary>
        /// Demonstrates the recommended usage pattern with using statements.
        /// Resources are explicitly disposed when using blocks exit, ensuring native resources are freed.
        /// </summary>
        public static void BasicUsageExample()
        {
            ExampleLog.Title("Basic Safe Usage Example");
            
            try
            {
                using var database = new Database(":memory:");
                using var connection = database.Connect();
                using var result = connection.Query("CREATE NODE TABLE Person(name STRING, age INT64, PRIMARY KEY(name))");
                if (result.IsSuccess)
                {
                    ExampleLog.Success("Table created successfully");
                }
                else
                {
                    ExampleLog.Error($"Failed to create table: {result.GetErrorMessage()}");
                }
            }
            catch (KuzuException ex)
            {
                ExampleLog.Error($"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                ExampleLog.Error($"Unexpected error: {ex.Message}");
            }
            ExampleLog.Separator();
        }

        /// <summary>
        /// Demonstrates that resources must be explicitly disposed even when exceptions occur.
        /// Using try-finally or using statements ensures cleanup happens.
        /// </summary>
        public static void ExceptionSafetyExample()
        {
            ExampleLog.Title("Exception Safety Example");
            
            try
            {
                using var database = new Database(":memory:");
                using var connection = database.Connect();
                throw new InvalidOperationException("Simulated exception");
            }
            catch (InvalidOperationException ex)
            {
                ExampleLog.Success($"Exception occurred: {ex.Message}");
                ExampleLog.Info("Resources with using will automatically be disposed of");
            }
            ExampleLog.Separator();
        }

        /// <summary>
        /// Demonstrates safe usage with multiple connections.
        /// Shows that database can be safely shared between connections when properly disposed.
        /// </summary>
        public static void MultipleConnectionsExample()
        {
            ExampleLog.Title("Multiple Connections Example");
            try
            {
                using var database = new Database(":memory:");
                using var connection1 = database.Connect();
                using var connection2 = database.Connect();
                ExampleLog.Success("Created multiple connections successfully");
            }
            catch (KuzuException ex)
            {
                ExampleLog.Error($"Database error: {ex.Message}");
            }
            ExampleLog.Separator();
        }


        /// <summary>
        /// Demonstrates the CORRECT way to handle resources without using statements.
        /// This pattern requires careful manual disposal but may be necessary in some scenarios.
        /// </summary>
        public static void ManualDisposalPattern()
        {
            ExampleLog.Title("Manual Disposal Pattern (Advanced)");
            Database? database = null;
            Connection? connection = null;
            QueryResult? result = null;
            
            try
            {
                database = new Database(":memory:");
                connection = database.Connect();
                result = connection.Query("CREATE NODE TABLE Test(id INT64, PRIMARY KEY(id))");
                ExampleLog.Success("Resources created and used successfully");
            }
            catch (Exception ex)
            {
                ExampleLog.Error($"Error: {ex.Message}");
            }
            finally
            {
                result?.Dispose();
                connection?.Dispose();
                database?.Dispose();
                ExampleLog.Success("All resources manually disposed in proper order");
            }
            ExampleLog.Separator();
        }
    }
}