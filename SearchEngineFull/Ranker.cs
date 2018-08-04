using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    public class Ranker
    {
        Dictionary<string, Dictionary<string, int>> QueryDictionary;
        Dictionary<string, TermData> MainDictionary;
        public Dictionary<string, DocumentData> DocInfo;
        // the frequency of each term in the query
        Dictionary<string, int> frequencyTermInQuery;
        int avergeDocLen;
        double b = 0.75;
        double k1 = 1.2;
        double k;
        double ri = 0;
        double R = 0;
        double k2 = 700;
        int NumDocsIncollection;

        public Ranker(Dictionary<string, DocumentData> docInfo, Dictionary<string, TermData> mainDictionary)
        {
            MainDictionary = mainDictionary;
            DocInfo = docInfo;
            int sum = 0;
            NumDocsIncollection = docInfo.Count;
            foreach (string doc in docInfo.Keys)
            {
                sum = sum + docInfo[doc].Length;
            }
            avergeDocLen = sum / NumDocsIncollection;

        }
        public List<KeyValuePair<string, double>> rank(Dictionary<string, Dictionary<string, int>> queryDictionary, Dictionary<string, int> afterParse)
        {
            double k;
            frequencyTermInQuery = afterParse; // the frequency of each term in the query
            QueryDictionary = queryDictionary;
            Dictionary<string, double> ranksScoring = new Dictionary<string, double>();
            Dictionary<string, int> docs;
            double tmp1;
            double tmp2;
            double tmp3;
            double score;
            foreach (string term in queryDictionary.Keys)
            {
                int ni = MainDictionary[term].Df;
                docs = queryDictionary[term];
                foreach (string d in docs.Keys)
                {
                    int DL = DocInfo[d].Length;
                    k = (((DL / avergeDocLen) * b) + (1 - b)) * k1;
                    int qfi = frequencyTermInQuery[term];


                    double fi = docs[d];
                    tmp1 = ((ri + 0.5) / (R - ri + 0.5)) / ((ni - ri + 0.5) / (NumDocsIncollection - ni - R + ri + 0.5));
                    tmp1 = Math.Log(tmp1);
                    tmp2 = ((k1 + 1) * fi) / (k + fi);
                    tmp3 = (k2 + 1) * qfi / (k2 + qfi);
                    score = tmp1 * tmp2 * tmp3;
                    if (ranksScoring.ContainsKey(d) == false)
                    {
                        ranksScoring.Add(d, score);

                    }
                    else

                        ranksScoring[d] = ranksScoring[d] + score;
                }
            }


            var ordered = ranksScoring.OrderBy(x => x.Value);

            return ordered.ToList();


        }


    }
}
