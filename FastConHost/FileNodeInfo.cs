using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FastConHost
{
    public class FileNodeInfo
    {
        public FileNodeInfo(long id, long parentId, string fileName)
        {
            FileNodeId = id;
            ParentDirectoryId = parentId;
            FileName = fileName;
        }

        public DateTime LastModifiedTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public long FileNodeId { get; set; }
        public long ParentDirectoryId { get; set; }
        public string FileName { get; set; }
        public List<string> BreakWords()
        {
            List<string> words = new List<string>();
            return words;
        }
    }
}
