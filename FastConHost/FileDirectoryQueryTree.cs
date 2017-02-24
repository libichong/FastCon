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
        public Dictionary<long, FSNode> FRNLookup;

        public FileDirectoryQueryTree()
        {
            fileTree = new Dictionary<long, FileNodeInfo>();
            dirTree = new Dictionary<long, DirectoryNodeInfo>();
            FRNLookup = new Dictionary<long, FSNode>();
            wordBreaker = new WordBreaker(@"C:\FastCon\Library\words.dll");
        }
        public void Dispose()
        {
        }

        public IEnumerable<string> LookUp(string query, bool isFile)
        {
            if(this.ContainsKey(query))
            {
                foreach(var id in this[query])
                {
                    if(isFile && FRNLookup[id].IsFile)
                    {
                        String topPar = null;
                        FSNode pa = FRNLookup[id];
                        FSNode temp = FRNLookup[id];
                        String partPath = null;

                        while (FRNLookup.TryGetValue(temp.ParentFRN, out temp))
                        {
                            pa = temp;
                            partPath = string.Concat(temp.FileName, @"\", partPath);
                        }

                        if (!pa.IsFile)
                        {
                            if (string.IsNullOrEmpty(partPath))
                            {
                                partPath = pa.FileName;
                                topPar = pa.FileName;
                            }
                            else
                            {
                                topPar = pa.FileName;
                            }
                        }

                        string fullDirectory = string.Concat(@"D:\\", partPath);
                        yield return fullDirectory;
                    }                    
                }
            }
        }

        public void Add(long id, string name)
        {
            var subWords = wordBreaker.BreakWorkds(name);

            foreach(var subWord in subWords)
            {
                var subWordLower = subWord.ToLower();
                if(!this.ContainsKey(subWordLower))
                {
                    this[subWordLower] = new HashSet<long>();
                }
                if (this.Values.Count < 100)
                {
                    this[subWordLower].Add(id);
                }
            }
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
