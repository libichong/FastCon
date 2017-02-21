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
            var files = (new DriveInfo(@"c:\")).EnumerateFiles().ToArray();
            var elapsed = sw.ElapsedMilliseconds.ToString();
            Console.WriteLine(string.Format("Found {0} files, elapsed {1} ms", files.Length, elapsed));

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
