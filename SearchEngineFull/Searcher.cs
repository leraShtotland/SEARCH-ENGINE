using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    public class Searcher
    {

        public Dictionary<string, TermData> MainDictionary;
        public Dictionary<string, DocumentData> DocInfo;
        string CorpusFolder;
        string postingPath;
        public Searcher(Dictionary<string, TermData> mainDictionary, string corpusFolder, bool stemmer, string postingsFolder, Dictionary<string, DocumentData> docInfo)
        {

            MainDictionary = mainDictionary;
            CorpusFolder = corpusFolder;
            if (stemmer == true)
                postingPath = postingsFolder + @"\with Stemmer";
            else postingPath = postingsFolder + @"\withOut Stemmer";
            postingPath = postingPath + @"\";
            DocInfo = docInfo;

        }

        public List<KeyValuePair<string, double>> search(string Query)
        {
            Dictionary<string, Dictionary<string, int>> QueryDictionary = new Dictionary<string, Dictionary<string, int>>();

            Parse p = new Parse(CorpusFolder, false);
            Ranker ranker = new Ranker(DocInfo, MainDictionary);
            Dictionary<string, int> afterParse = p.parseFile(new DocumentData(), Query);
            foreach (string term in afterParse.Keys)
            {

                QueryDictionary.Add(term, new Dictionary<string, int>());
                if (MainDictionary.ContainsKey(term))
                {
                    string path = MainDictionary[term].Path;
                    int line = MainDictionary[term].LineNum;
                    string output;
                    string[] splitedLine;
                    using (var sr = new StreamReader(postingPath + path + ".txt"))
                    {
                        for (int i = 1; i < line; i++)
                            sr.ReadLine();
                        output = sr.ReadLine();
                    }

                    splitedLine = output.Split('^', '~');
                    int length = splitedLine.Length;
                    length = length - 2;
                    for (int j = 1; j < length; j = j + 2)
                    {
                        QueryDictionary[term].Add(splitedLine[j], int.Parse(splitedLine[j + 1]));

                    }
                }

                else QueryDictionary[term] = null;



            }
            List<KeyValuePair<string, double>> queryResult = ranker.rank(QueryDictionary, afterParse);
            queryResult.Reverse();


            return queryResult;


        }

    }
}
