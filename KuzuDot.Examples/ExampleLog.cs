using System;

namespace KuzuDot.Examples
{
    internal static class ExampleLog
    {
        // Enable colors unless output is redirected (avoids ANSI issues) and avoid const to prevent unreachable warnings.
        private static bool UseColors = !Console.IsOutputRedirected;
        private static object _lock = new object();
        public static void Title(string text)
        {
            Separator('=');
            WriteLine(text.ToUpperInvariant(), ConsoleColor.Cyan);
            Separator('=');
        }
        public static void Section(string text)
        {
            Separator('-');
            WriteLine(text, ConsoleColor.DarkCyan);
        }
        public static void Info(string text) => WriteLine("[INFO] " + text, ConsoleColor.Gray);
        public static void Success(string text) => WriteLine("[OK]   " + text, ConsoleColor.Green);
        public static void Warn(string text) => WriteLine("[WARN] " + text, ConsoleColor.Yellow);
        public static void Error(string text) => WriteLine("[ERR]  " + text, ConsoleColor.Red);
        public static void Separator(char c='=') => WriteLine(new string(c, 60), ConsoleColor.DarkGray);

        private static void WriteLine(string text, ConsoleColor color)
        {
            lock(_lock)
            {
                if (!UseColors) { Console.WriteLine(text); return; }
                var prev = Console.ForegroundColor;
                try { Console.ForegroundColor = color; Console.WriteLine(text); }
                finally { Console.ForegroundColor = prev; }
            }
        }

        // Safe step helpers -------------------------------------------------
        public static void SafeStep(string description, Action action)
        {
            try { Info(description + " ..."); action(); Success(description); }
            catch (Exception ex) { Error(description + " FAILED: " + ex.Message); throw; }
        }
        public static T SafeCreate<T>(string description, Func<T> factory) where T: class
        {
            try { Info(description + " ..."); var v = factory(); Success(description); return v; }
            catch(Exception ex) { Error(description + " FAILED: " + ex.Message); throw; }
        }
    }
}
