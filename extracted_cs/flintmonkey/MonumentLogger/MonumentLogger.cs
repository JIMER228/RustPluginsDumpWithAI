/*
    MonumentLogger Plugin
    Version: 1.0.7
    Author: FlintMonkey

    Description:
    Lists all monuments on current Rust server map in console via command including prefab names, locations, and total count.

    License:
    This project is licensed under the MIT License.
    You are free to use, modify, and distribute this software, as long as you include this notice and the following disclaimer:

    MIT License
    Copyright (c) 2024 FlintMonkey

    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
    to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
    and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace Oxide.Plugins
{
    [Info("MonumentLogger", "FlintMonkey", "1.0.7")]
    [Description("Logs all monuments via command")]

    public class MonumentLogger : RustPlugin
    {
        // Command registration for both Carbon and Oxide/uMod frameworks
        [ConsoleCommand("monument_list")]
        private void MonumentListCommand(ConsoleSystem.Arg arg)
        {
            if (IsCarbon())
            {
                // Handle command for Carbon framework
                LogMonuments();
            }
            else
            {
                // Handle command for Oxide/uMod framework
                Puts("Listing all monuments:");
                LogMonuments();
            }
        }

        // Method that logs all monuments to the console
        private void LogMonuments()
        {
            Puts("[Monument Logger start]"); // Start marker

            int count = 0; // Counter for total monuments

            foreach (var monument in TerrainMeta.Path.Monuments)
            {
                count++; // Increment the counter
                var position = monument.transform.position; // Get monument position
                Puts($"Name: {monument.name}\nPrefab: {monument}\nLocation: {position}\n");
            }

            Puts($"Total Monuments: {count}"); // Print total count
            Puts("[Monument Logger end]"); // End marker
        }

        // Helper method to detect if we're running on the Carbon framework
        private bool IsCarbon()
        {
            return plugins.Exists("CarbonCore");
        }
    }
}
