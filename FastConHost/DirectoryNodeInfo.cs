using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastConHost
{
    public class DirectoryNodeInfo
    {
        public DirectoryNodeInfo()
        {
            ChildDirectoryIds = new List<long>();
        }

        public DateTime LastModifiedTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public long DirectoryNodeId { get; set; }
        public long ParentDirectoryId { get; set; }
        public string DirectoryName { get; set; }
        public List<long> ChildDirectoryIds { get; set; }
        public List<string> BreakWords()
        {
            List<string> words = new List<string>();
            return words;
        }
    }
}
