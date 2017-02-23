using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FastConHost
{
    public class WordBreaker
    {
        private static bool fInitialized = false;
        private string DictionartPath = string.Empty;
        private HashSet<string> DictionarySet = new HashSet<string>();
        private const string InvalidPathCharcter = "\\/:*?\"<>|";
        public WordBreaker(string dictionaryFile)
        {
            this.DictionartPath = dictionaryFile;
            if(File.Exists(dictionaryFile))
            {
                fInitialized = LoadDictionary(dictionaryFile);
            }
        }

        private bool IsValidPath(string word)
        {
            for (int i = 0; i < InvalidPathCharcter.Length; i++)
            {
                if (word.Contains(InvalidPathCharcter[i].ToString()))
                {
                    return false;
                }
            }
            return true;
        }

        public List<string> BreakWorkds (string word)
        {
            List<string> subWords = new List<string>();
            subWords.Add(word);
            int pos = 0;
            for(int i = pos + 1;i<word.Length;i++)
            {
                if((word[i] >='A' && word[i] <= 'Z') || word[i] == '_' || word[i] ==' ' || word[i] == '-')
                {
                    subWords.Add(word.Substring(pos, i - pos));
                    pos = i;
                }
            }

            if(pos <  word.Length)
            {
                subWords.Add(word.Substring(pos, word.Length - pos));
            }

            //List<bool> f = new List<bool>(word.Length + 1);
            //List<List<bool>> prev = new List<List<bool>>();
            //for (int i = 0; i < word.Length + 1; i++)
            //{
            //    prev.Add(new List<bool>());
            //    for (int j = 0; j < word.Length; j++)
            //    {
            //        prev[i].Add(false);
            //    }
            //}

            //f[0] = true;

            //for(int i =1;i<word.Length;i++)
            //{
            //    for(int j= i-1;j>0;--j)
            //    {
            //        if(f[j] && DictionarySet.Contains(word.Substring(j, i-j)))
            //        {
            //            f[i] = true;
            //            prev[i][j] = true;
            //        }
            //    }
            //}

            //List<string> path = new List<string>();
            //gen_path(word, ref prev, word.Length, ref path, ref subWords);
            return subWords;
        }

        private void gen_path(string word, ref List<List<bool>> prev, int cur, ref List<string> path, ref List<string> result)
        {
            if(cur == 0)
            {
                string tmp = string.Empty;
            }
        }

        private bool LoadDictionary(string dictionaryFile)
        {
            try
            {
                FileStream aFile = new FileStream(dictionaryFile, FileMode.Open);
                StreamReader sr = new StreamReader(aFile);
                string strLine = sr.ReadLine();
                while (strLine != null)
                {
                    strLine = sr.ReadLine();
                    if (!string.IsNullOrEmpty(strLine) && IsValidPath(strLine) && strLine.Length > 1)
                    {
                        DictionarySet.Add(strLine);
                    }
                }
                sr.Close();
            }
            catch (IOException ex)
            {
                return false;
            }
            return true;
        }
    }
}
