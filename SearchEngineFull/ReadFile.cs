using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace SearchEngine
{
    class ReadFile
    {
        Indexer indexer;
        Parse parser;

        public Dictionary<string, Dictionary<string, int>> afterDictionary;
        public Dictionary<string, TermData> mainDictionary { get; set; }
        //list of all the terms in the mainDictionary 
        public List<string> termList { get; set; }
        // the number of docs in the corpus
        public int docsCounter { get; set; }
        Dictionary<string, StringBuilder> tmpDic;
        string content;
        public HashSet<string> languages;
        string inputFolderPath;
        string postingPath;
        int numFiles;


        public ReadFile(string inputPath, string postingFolder, bool stemmer)
        {
            afterDictionary = new Dictionary<string, Dictionary<string, int>>();
            docsCounter = 0;
            numFiles = 0;
            if (stemmer == true)
                postingPath = postingFolder + @"\with Stemmer";
            else postingPath = postingFolder + @"\withOut Stemmer";
            System.IO.Directory.CreateDirectory(postingPath);
            inputFolderPath = inputPath;
            indexer = new Indexer(postingPath);
            parser = new Parse(inputPath, stemmer);
            languages = new HashSet<string>();
            mainDictionary = new Dictionary<string, TermData>();
            termList = new List<string>();

        }

        public void MainProcess()
        {
            string currentLang = "";
            numFiles = 0;

            //Dictionary we get from parser
            Dictionary<string, int> docTermsDic = new Dictionary<string, int>();
            //Dictionary send to indexer
            tmpDic = new Dictionary<string, StringBuilder>();

            // set regregular expression
            string regDOC = @"<DOC>(.*\n)+?</DOC>";
            string regTEXT = @"<TEXT>(.*\n)+?</TEXT>";
            string regDocNO = @"<DOCNO> \w+-{0,1}\w+ </DOCNO>";
            string regLanguage = @"<F P=105>\s*\w+\s+\w*\s*\w*\s*\w*\s*\w*\s*\w*\s*</F>";
            string regDocDate = @"<DATE1>  \d{0,1} \w+ \d{4} </DATE1>";

            foreach (string file in Directory.EnumerateFiles(inputFolderPath))
            {
                if (file != inputFolderPath + @"\stop_words.txt")
                {
                    //file text
                    content = File.ReadAllText(file);
                    MatchCollection mcDOC = Regex.Matches(content, regDOC);
                    for (int i = 0; i < mcDOC.Count; i++)
                    {
                        //restart document's info
                        DocumentData[] docsInfo = RestartDocInfo(mcDOC);

                        //get the document's language if exists
                        currentLang = AddLanguage(currentLang, regLanguage, mcDOC, i, docsInfo);

                        //get the document's name (DocNO)
                        AddDate(regDocNO, regDocDate, mcDOC, i, docsInfo);

                        //get the document's name (DocNO)
                        MatchCollection mcText = Regex.Matches(mcDOC[i].ToString(), regTEXT);
                        foreach (Match text in mcText)
                        {
                            docsInfo[i].Length = 0;
                            //parse the text docs 
                            docTermsDic = parser.parseFile(docsInfo[i], text.ToString());
                            foreach (string term in docTermsDic.Keys)
                            {
                                AddTermToTempDic(docTermsDic, i, docsInfo, term);
                            }
                        }
                        if (parser.parserList.Count > 0)
                            ///get the terms list
                            addParseList(parser.parserList);
                    }
                    //for 10 files run indexer
                    if (numFiles % 10 == 0)
                    {
                        RunIndexDocs();
                    }
                    numFiles++;
                }
            }

            SaveAfterDictionary();
            //save the Terms Dictionary
            SaveTheDictionary();
            //save the docoments Dictionary
            SaveDocDictionary();

            saveLanguageFile();

        }

        private void SaveAfterDictionary()
        {
            StreamWriter sw = new StreamWriter(postingPath + @"\after.txt");
            string line = "";
            Dictionary<string, int> currDic = new Dictionary<string, int>();
            foreach (string term in afterDictionary.Keys)
            {
                currDic = afterDictionary[term];
                line = term;
                var ordered = currDic.OrderBy(x => x.Value);
                KeyValuePair<string, int>[] arr = ordered.ToArray();
                if (arr.Length >= 5)
                {
                    for (int t = 0; t < 5; t++)
                    {
                        line = line + "~" + arr[arr.Length - 1 - t].Key + "~" + arr[arr.Length - 1 - t].Value;

                    }

                }
                else
                {
                    for (int k = arr.Length - 1; k >= 0; k--)
                    {
                        line = line + "~" + arr[k].Key + "~" + arr[k].Value;

                    }
                }
                sw.WriteLine(line);
                sw.Flush();

            }
            sw.Close();

        }

        private void addParseList(List<string> parserList)
        {
            string currTerm = parserList[0];
            string nextTerm = "";
            for (int idx = 0; idx < parserList.Count - 1; idx++)
            {
                nextTerm = parserList[idx + 1];
                if (!afterDictionary.ContainsKey(currTerm))
                {
                    Dictionary<string, int> tmpDic = new Dictionary<string, int>();
                    tmpDic.Add(nextTerm, 1);
                    afterDictionary.Add(currTerm, tmpDic);
                }
                else if (afterDictionary[currTerm].ContainsKey(nextTerm))
                {
                    afterDictionary[currTerm][nextTerm] += 1;
                }
                else
                    afterDictionary[currTerm].Add(nextTerm, 1);
                currTerm = nextTerm;
            }

        }


        /// Restart Doc Info about each doc in the file
        private static DocumentData[] RestartDocInfo(MatchCollection mcDOC)
        {
            DocumentData[] docsInfo = new DocumentData[mcDOC.Count];
            for (int j = 0; j < docsInfo.Length; j++)
            {
                docsInfo[j] = new DocumentData();
            }
            return docsInfo;
        }
        /// add data for each doc to doc info
        private void AddDate(string regDocNO, string regDocDate, MatchCollection mcDOC, int i, DocumentData[] docsInfo)
        {
            MatchCollection mcDocName = Regex.Matches(mcDOC[i].ToString(), regDocNO);

            foreach (Match docNO in mcDocName)
            {
                docsCounter++;
                string[] splitedDocno = docNO.ToString().Split(' ');
                docsInfo[i].Name = splitedDocno[splitedDocno.Length - 2];
            }

            //Get the document's date
            MatchCollection mcDocDate = Regex.Matches(mcDOC[i].ToString(), regDocDate);

            foreach (Match docDate in mcDocDate)
            {
                string[] splitedDocno = docDate.ToString().Split(' ');
                String parsedDay = splitedDocno[2];
                if (splitedDocno[2].Length == 1)
                {
                    parsedDay = "0" + splitedDocno[2];
                }
                string theDate = splitedDocno[4] + "-" + parser.months[splitedDocno[3].ToLower()] + "-" + parsedDay;
                docsInfo[i].Date = theDate;
            }
        }
        // add the Language of each document to the document Data of  the document
        private string AddLanguage(string currentLang, string regLanguage, MatchCollection mcDOC, int i, DocumentData[] docsInfo)
        {
            MatchCollection mcLAnguage = Regex.Matches(mcDOC[i].ToString(), regLanguage);
            foreach (var language in mcLAnguage)
            {
                currentLang = "";
                string[] splitedLang = language.ToString().Split(' ');
                for (int k = 2; k < splitedLang.Length - 1; k++)
                {
                    string str = splitedLang[k].ToLower();
                    if (str.All(char.IsLetter) && str != "")
                    {
                        currentLang = currentLang + str + " ";
                        if (!languages.Contains(str) && (str.Equals("and") == false))
                            languages.Add(str);
                    }
                }
                if (currentLang == "")
                    docsInfo[i].Language = "";
                else
                {
                    docsInfo[i].Language = currentLang;
                }
            }
            return currentLang;
        }


        /// add term to the Dictionary that will send to indexer and all the information about the term to relevant data structures
        private void AddTermToTempDic(Dictionary<string, int> docTermsAndTf, int i, DocumentData[] docsInfo, string term)
        {
            if (tmpDic.ContainsKey(term))
            {
                tmpDic[term].Append(docsInfo[i].Name + "^" + docTermsAndTf[term] + "^");
                mainDictionary[term].Df = mainDictionary[term].Df + 1;
                mainDictionary[term].NumInCorpus = mainDictionary[term].NumInCorpus + docTermsAndTf[term];
            }
            else
            {
                if (mainDictionary.ContainsKey(term) == false && term != "")
                {
                    mainDictionary.Add(term, new TermData());
                    mainDictionary[term].Df = 1;
                    mainDictionary[term].NumInCorpus = docTermsAndTf[term];
                    tmpDic.Add(term, new StringBuilder().Append(docsInfo[i].Name + "^" + docTermsAndTf[term] + "^"));

                }
                else if (term != "")
                {
                    mainDictionary[term].Df++;
                    mainDictionary[term].NumInCorpus = mainDictionary[term].NumInCorpus + docTermsAndTf[term];
                    tmpDic.Add(term, new StringBuilder().Append(docsInfo[i].Name + "^" + docTermsAndTf[term] + "^"));
                }
            }
            //add the number of terms in the document to the document Length
            docsInfo[i].Length = docsInfo[i].Length + docTermsAndTf[term];
        }

        /// send the temp Dictionary for index
        private void RunIndexDocs()
        {
            int postNum = 0;
            indexer.indexDocs(tmpDic, mainDictionary);
            postNum++;
            tmpDic = new Dictionary<string, StringBuilder>();
        }



        // Save the information about all the docoments in a file
        private void SaveDocDictionary()
        {
            Dictionary<string, DocumentData> docInfo = parser.docInfo;
            string[] docinfoStrings = new string[docInfo.Count];
            int docs = 0;
            foreach (string doc in docInfo.Keys)
            {
                docinfoStrings[docs] = doc + "~" + docInfo[doc].NumUniqueTerm + "^" + docInfo[doc].max_tf + "^" + docInfo[doc].Language + "^" + docInfo[doc].Date + "^" + docInfo[doc].Length;
                docs++;
            }
            File.WriteAllLines(postingPath + @"\" + "docsInfo.txt", docinfoStrings);
        }

        /// save all the terms and the information about the terms to a file 
        private void SaveTheDictionary()
        {
            int termsNum = 0;
            //sort the Dictionary
            termList = mainDictionary.Keys.ToList();
            termList.Sort();
            //save the terms Dictionary
            string[] termInfo = new string[mainDictionary.Count];

            foreach (string term in termList)
            {
                termInfo[termsNum] = term + "^" + mainDictionary[term].Df + "^" + mainDictionary[term].Path + "^" + mainDictionary[term].LineNum + "^" + mainDictionary[term].NumInCorpus;
                termsNum++;
            }

            File.WriteAllLines(postingPath + @"\" + "Dictionary.txt", termInfo);
        }

        private void saveLanguageFile()
        {
            StreamWriter sw = new StreamWriter(@"c:\users\noa\desktop\langs.txt");
            foreach (string lang in languages)
            {
                sw.WriteLine(lang);

                sw.Flush();

            }
        }
       











    }
}


