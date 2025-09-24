using System;
using JetBrains.Annotations;
using Oxide.Core.Plugins;
using Oxide.Ext.ConsoleExt;

namespace Oxide.Plugins
{
    [Info("(Sample) ConsoleExt", "Ilovepatatos", "1.0.0")]
    public class ConsoleExtSample : CovalencePlugin
    {
#region Debug

        private static void Log(object msg, string color = OxideConsole.NORMAL)
        {
            OxideConsole.Log($"[(Sample) ConsoleExt] {msg}", color);
        }

        private static void LogFormat(string format, string color, params object[] args)
        {
            OxideConsole.LogFormat($"[(Sample) ConsoleExt] {format}", color, args);
        }

#endregion

#region Hooks

        [UsedImplicitly]
        [HookMethod(nameof(OnServerInitialized))]
        private void OnServerInitialized()
        {
            Log("--- Starting Format Tests ---", OxideConsole.GREEN);

            // ✅ Should format correctly
            ValidateLogFormat(false, "Hello {0}!", "world"); // Hello world!
            ValidateLogFormat(false, "Too many args {0}", "works fine", "error!", "another error!"); // Too many args zero
            ValidateLogFormat(false, "Escaped braces {{0}} = {0}", "value"); // Escaped braces {0} = value
            ValidateLogFormat(false, "Multiple {0} {1}", "a", "b"); // Multiple a b
            ValidateLogFormat(false, "Edge case: {{{0}}}", "x"); // Edge case: {x}
            ValidateLogFormat(false, "Double escaped: {{{{0}}}} = {0}", "v"); // Double escaped: {{0}} = v

            Log("");

            // ❌ Should throw with string.Format
            ValidateLogFormat(true, "Missing arg {0} {1}", "works!"); // throws (missing {1})
            ValidateLogFormat(true, "Invalid token: {abc}", "hello"); // throws (invalid format string)
            ValidateLogFormat(true, "Unmatched open brace: {0 {1}", "a", "b"); // throws (missing closing brace)
            ValidateLogFormat(true, "Unmatched close brace: {0}}", "x"); // throws (extra closing brace)

            Log("--- Completed Format Tests ---", OxideConsole.GREEN);
        }

#endregion

#region Methods

        private static void ValidateLogFormat(bool isExpectingThrow, string format, params object[] args)
        {
            try
            {
                string _ = string.Format(format, args);

                if (isExpectingThrow)
                {
                    Log($"Format did not throw '{format} {string.Join(", ", args)}'", OxideConsole.LIGHT_RED);
                }
                else
                {
                    LogFormat(format, OxideConsole.LIGHT_GREEN, args);
                }
            }
            catch (Exception ex)
            {
                if (isExpectingThrow)
                {
                    LogFormat(format, OxideConsole.LIGHT_GREEN, args);
                }
                else
                {
                    Log($"Error while logging format '{format} {string.Join(", ", args)}': {ex.Message}", OxideConsole.RED);
                }
            }
        }

#endregion
    }
}