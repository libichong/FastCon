using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace FastConHost
{
    public class FileDirectoryQueryTree : Dictionary<string, HashSet<long>>, IDisposable
    {
        private Dictionary<long, FileNodeInfo> fileTree;
        private Dictionary<long, DirectoryNodeInfo> dirTree;
        private WordBreaker wordBreaker;

        public FileDirectoryQueryTree()
        {
            fileTree = new Dictionary<long, FileNodeInfo>();
            dirTree = new Dictionary<long, DirectoryNodeInfo>();
            wordBreaker = new WordBreaker(@"C:\FastCon\Library\words.dll");
        }
        public void Dispose()
        {
        }

        public void Add(DirectoryNodeInfo dir)
        {

            //var name = dir.Name.ToLower();
            //if (!this.ContainsKey(name))
            //{
            //    this[name] = new HashSet<long>();
            //}
            var subWords = wordBreaker.BreakWorkds(dir.DirectoryName);

            foreach(var subWord in subWords)
            {
                if(!this.ContainsKey(subWord))
                {
                    this[subWord] = new HashSet<long>();
                }
                this[subWord].Add(dir.DirectoryNodeId);
            }

            //if (!this[name].Contains(dir.FullName))
            //{
            //    this[name].AddLast(dir.FullName);
            //}
        }

        public void Add(FileNodeInfo fileNode)
        {
            fileTree[fileNode.FileNodeId] = fileNode;
        }

        public void Serialize(Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(this.Count);
            foreach (var kvp in this)
            {
                writer.Write(kvp.Key.ToLower());
                writer.Write(kvp.Value.Count());
                foreach (var path in kvp.Value)
                {
                    writer.Write(path);
                }
                writer.Flush();
            }
        }

        public void Merge(FileDirectoryQueryTree fastConTree)
        {
            foreach (var kvp in fastConTree)
            {
                if (this.ContainsKey(kvp.Key))
                {
                    foreach (var path in kvp.Value)
                    {
                        if (!this[kvp.Key].Contains(path))
                        {
                            //this[kvp.Key].AddLast(path);
                        }
                    }
                }
                else
                {
                    this[kvp.Key] = new HashSet<long>();
                    foreach (var path in kvp.Value)
                    {
                        //this[kvp.Key].AddLast(path);
                    }
                }
            }
        }

        public void Deserialize(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            int count = reader.ReadInt32();
            for (int n = 0; n < count; n++)
            {
                var key = reader.ReadString();
                if (!this.ContainsKey(key))
                {
                    this[key] = new HashSet<long>();
                }
                var num = reader.ReadInt32();
                for (int i = 0; i < num; i++)
                {
                    var path = reader.ReadString();
                    //this[key].AddLast(path);
                }
            }
        }
    }
}
