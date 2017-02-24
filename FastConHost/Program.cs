using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace FastConHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var sw = Stopwatch.StartNew();
            var scanner = new MFTScanner();
            var files = scanner.BuildIndex("D:", null).ToArray();
            //var files = (new DriveInfo(@"d:\Work\")).EnumerateFiles().ToArray();
            var elapsed = sw.ElapsedMilliseconds.ToString();
            Console.WriteLine(string.Format("Found {0} files, elapsed {1} ms", files.Length, elapsed));

            while(true)
            {
                Console.WriteLine("Query: ");
                string query = Console.ReadLine();
                scanner.LookUp(query);
            }

            //WordBreaker wordBreaker = new WordBreaker(@"C:\FastCon\Library\words.dll");
            //foreach(var line in Utility.EnumerateFiles("C:\\Windows"))
            //{
            //    Console.WriteLine(line);
            //}

            //Utility.PathCollector collectorCallBack = new Utility.PathCollector();
            //USNHelper helper = new USNHelper("C:\\Windows", "C:\\");

            //helper.GetFileSystemEntries("C:\\Windows");
        }
    }
}
