using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{

    class Indexer
    {
        string path;
        char[] alpha;
        bool let;
        string path_old;
        string path_new;
        int countLines;

        public Indexer(string postingPath)
        {
            path = postingPath + @"\";
            alpha = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
            //create abc files
            for (int i = 0; i < 26; i++)
            {
                var myFile = File.Create(path + alpha[i] + ".txt");
                myFile.Close();
                
            }
            // crete file for not abc terms
            var otherFile = File.Create(path + "other" + ".txt");
            otherFile.Close();
        }

        //merge between terms of 10 files after paesing and the posting files. 
        public void indexDocs(Dictionary<string, StringBuilder> termToMerge, Dictionary<string, TermData> mainDictionary)
        {
            string[] toAdd;
            int size = termToMerge.Count;
            toAdd = new string[size];
            string line = null;    
            char letter;
            //put all the new terms in toAdd array
            CreteToAddArray(termToMerge, mainDictionary, toAdd);
            for (int i = 0; i < 27; i++)
            {
                letter = '\0';
                if (i < 26)
                    letter = alpha[i];
                countLines = 1;
                //set the paths of posting file  i
                SetPaths(i);
                using (StreamReader sr = new StreamReader(path_old))
                {
                    using (StreamWriter sw = new StreamWriter(path_new))
                    {
                        //read the first line from the file
                        line = sr.ReadLine();
                        while (line != null)
                        {
                            //get the term from the line
                            string t = getTerm(line);
                            //check if the line should be update
                            if (termToMerge.ContainsKey(t))
                            {
                                line = line + termToMerge[t];
                            }
                            //write the line to the new file
                            sw.WriteLine(line);
                            //read the next line from the file
                            line = sr.ReadLine();
                            countLines++;
                        }
                        for (int r = 0; r < toAdd.Length; r++)
                        {
                            // marge the terms in to add array with the posting file 
                            MergeTermsWithPosting(termToMerge, mainDictionary, toAdd, letter, i, sw, r);
                        }
                    }
                }
                //delete the old file and rename the new to old
                changeFiles(path_old, path_new);

            }
        }

        //put all the new terms in toAdd array
        private void CreteToAddArray(Dictionary<string, StringBuilder> termToAdd, Dictionary<string, TermData> termDoc, string[] toAdd)
        {
           int toAddInt = 0; 
            foreach (string term in termToAdd.Keys)
            {
                if (termDoc[term].Path == null)
                {
                    toAdd[toAddInt] = term;
                    toAddInt++;
                }
            }
        }

        //set the paths of posting file  i
        private void SetPaths(int i)
        {
            path_old = "";
            path_new = "";
            if (i < 26)
            {
                path_old = path + alpha[i] + ".txt";
                path_new = path + "new" + alpha[i] + ".txt";
            }
            else if (i == 26)
            {
                path_old = path + "other" + ".txt";
                path_new = path + "new" + "other" + ".txt";

            }
        }

        //get the term from the line
        private static string getTerm(string line)
        {
            int l = line.IndexOf("~");
            string t = line.Substring(0, l);
            return t;
        }

        // marge the terms in to add array with the posting file 
        private int MergeTermsWithPosting(Dictionary<string, StringBuilder> termToAdd, Dictionary<string, TermData> termDoc, string[] toAdd, char letter, int i, StreamWriter sw, int r)
        {
            string term = toAdd[r];
            if (term != null && term.Length > 0)
            {
                let = char.IsLetter(term[0]);
                // if the term is word
                if (let && term[0] == letter)
                {
                    termDoc[term].Path = term[0] + "";
                    termDoc[term].LineNum = countLines;
                    sw.WriteLine(term + "~" + termToAdd[term].ToString());
                    countLines++;
                }
                //if the term not a word
                else if (let == false && (i == 26))
                {
                    termDoc[term].Path = "other";
                    termDoc[term].LineNum = countLines;
                    sw.WriteLine(term + "~" + termToAdd[term].ToString());
                    countLines++;
                }
            }
            return countLines;
        }

        //delete the old file and rename the new to old
        private static void changeFiles(string path_old, string path_new)
        {
            File.Delete(path_old);
            if (File.Exists(path_old))
                File.Delete(path_old);
            System.IO.File.Move(path_new, path_old);
        }


    }
}



