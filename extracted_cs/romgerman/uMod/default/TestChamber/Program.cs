using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Oxide.Core;

namespace TestChamber
{
    class Program
    {
        static void Main(string[] args)
        {
            Interface.DebugCallback += (str) =>
            {
                Console.WriteLine(str);
            };

            Interface.Initialize();

            Console.ReadKey();
        }
    }
}
