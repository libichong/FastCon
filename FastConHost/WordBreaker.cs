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
