using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SearchEngine
{

    //class 
    class Parse
    {
        public List<string> parserList;
        HashSet<string> stopWords; //stores the stopwords
        Dictionary<string, int> termsDic; //store the terms and the frequency for curren document
        public Dictionary<string, DocumentData> docInfo; //store the terms' info
        public Dictionary<string, string> months; //store the mapping between monthes name to numbers
        bool stemBool; // true if stemming is needed
        Stemmer stemmer; // a stemmer instance     
        private string[] splited; //store the splited document's content
        int maxFrecInt;

        //a constractor
        public Parse(string swPath, bool stem)
        {
            parserList = new List<string>();
            string line;
            stemBool = stem;
            docInfo = new Dictionary<string, DocumentData>();
            stopWords = new HashSet<string>();

            StreamReader file = new StreamReader("stop_words.txt");
            stemmer = new Stemmer();

            //read the stopwords file and add them to the hashSet
            while ((line = file.ReadLine()) != null)
            {
                stopWords.Add(line.ToLower());
            }

            //initialize the monthes dictionary
            months = new Dictionary<string, string>();

            addMonthes();

        }


        //Parse a document
        public Dictionary<string, int> parseFile(DocumentData docData, string content)
        {
            parserList = new List<string>();
            //add the data from the readFile instance
            docInfo[docData.Name] = docData;
            termsDic = new Dictionary<string, int>();
            maxFrecInt = 0;
            string[] delimiters = { " ", "\r\n", "\n", "--" };
            string word;
            int i = 0;

            //split the document's content into a string array
            splited = content.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            string termStr = "";

            //indicate if the word end with puncuation
            bool chopedLast = false;

            //indicate if the word already parsed
            bool parsed = false;

            //find match role for each term
            while (i < splited.Length)
            {

                parsed = false;
                chopedLast = false;

                //chop the start of the word if it contain un wanted characters
                word = chopStart(splited[i]);


                while (word != "" && toRemoveLast(word))
                {
                    chopedLast = true;
                    word = word.Substring(0, word.Length - 1);
                }


                //delete un wanted character 
                word = toRemoveAny(word);

                //if the word not conatain any word or digit it not parsed
                if (word == "" || !(word.Any(char.IsLetterOrDigit)))
                    i++;

                //not parse the word "Language"  ????
                else if (word == "Language")
                    i++;


                else if (splited[i] == "<F")
                {
                    while (i < splited.Length - 1 && splited[i] != "</F>")
                        i++;
                    i++;
                }

                //check if the word is the article type
                else if (word == "Article" && splited[i + 1].StartsWith("Type")) //////check if to save the Type
                {
                    i = i + 3;
                }


                else if (splited[i] == "[Text]")
                    i++;


                else if (splited[i][0] == '<' || splited[i][splited[i].Length - 1] == '>')
                    i++;

                //chack if the word starts a date and if so add the date to the dictionary
                else if (checkAndParseDate(ref i, docData.Name) == true) { }

                //check if the word match to the between rule
                else if (!parsed && !chopedLast && i < splited.Length - 3 && (word == "Between" || word == "between") && (splited[i + 2] == "And" || splited[i + 2] == "and") && (isNumber(splited[i + 1]) || isAfrac(splited[i + 1])) && (isNumberWithAfter(splited[i + 3]) || isAfracWithAfter(splited[i + 3])))
                {
                    string tmpS = splited[i + 3];

                    //chope the unnececery chars from the beginning of the word
                    while (toRemoveLast(tmpS))
                        tmpS = tmpS.Substring(0, tmpS.Length - 1);
                    termStr = "between " + parseNumber(splited[i + 1]) + " and " + parseNumber(tmpS);
                    i = i + 4;
                    addToDic(termStr, docData.Name);
                }

                //check if the word and those after it are contain only capital letters and store all consecutive at one term
                else if (!chopedLast && i < splited.Length && word.Length > 1 && (word.All(char.IsUpper)))
                {
                    handleCapitalLetters(docData, ref word, ref i);
                }

                //check if the word starts with $
                else if (word[0] == '$')
                {
                    handleDollarAtStart(docData, ref word, ref i, ref termStr);
                }



                //check if there is an hyphen
                else if (word.Contains('-') && !word.Contains("--") && word[0] != '-' && word[word.Length - 1] != '-')
                {
                    termStr = handleHyphen(word, ref i, ref chopedLast);

                    //store the new term in the parser's dictionary
                    addToDic(termStr.ToLower(), docData.Name);
                }

                //check if the wod is a fraction
                else if (isAfrac(word))
                {
                    termStr = word;
                    i++;
                    addToDic(termStr, docData.Name);
                }

                //check if the word is number
                else if (isNumber(word))
                {
                    termStr = handleNumber(docData, word, ref i, ref chopedLast);

                }

                //check if the word contain only letters
                else if (word.All(char.IsLetter))
                {
                    word = word.ToLower();
                    if (!stopWords.Contains(word))
                    {
                        termStr = word;// + "#\n";
                        addToDic(termStr.ToLower(), docData.Name);
                    }
                    i++;
                }

                //check if the word is a stopword 
                else if (stopWords.Contains(word.ToLower()))
                {
                    i++;
                }

                //if the word didnt match any rule add it to the dictionary
                else if (!parsed)
                {
                    termStr = word;
                    addToDic(termStr.ToLower(), docData.Name);
                    i++;
                }

            }

            //check if there is need to stem
            if (stemBool == true)
            {
                Dictionary<string, int> termsDicStemer = new Dictionary<string, int>();
                foreach (string term in termsDic.Keys)
                {
                    string stem = stemmer.stemTerm(term);
                    if (termsDicStemer.ContainsKey(stem))
                        termsDicStemer[stem] = termsDicStemer[stem] + termsDic[term];
                    else
                        termsDicStemer.Add(stem, termsDic[term]);
                }
                //return the dictionary after stemminfg
                return termsDicStemer;
            }
            docInfo[docData.Name].max_tf = maxFrecInt;

            //return the terms' dictionary
            return termsDic;
        }






        //handle prices
        private void handleDollarAtStart(DocumentData docData, ref string word, ref int i, ref string termStr)
        {
            if (!word.Contains('-'))
            {
                //chop unnececery chars from the rnd of the word
                while (toRemoveLast(word))
                    word = word.Substring(0, word.Length - 1);

                //chop the $ from the beginning
                word = word.Substring(1);
                if (isAfrac(word))
                {
                    termStr = word;
                    i++;
                }
                else if (!isNumber(word))
                {
                    i++;
                    //if the word is not number add it as a term
                    addToDic(word.ToLower(), docData.Name);
                }
                //if the word is valid number
                else
                {
                    //check if thenumber bigger then a million
                    if (!isBigAfter(ref i))
                    {
                        termStr = parseNumber(word);
                        i++;
                        if (isAfracWithAfter(splited[i]))
                        {
                            termStr += parseFrac(ref i);
                            i++;
                        }
                    }
                    else
                    {
                        termStr = parseBig(word, ref i);
                        i += 2;
                    }

                }

                // add "dollars" to the end of the string
                termStr += " dollars";
                addToDic(termStr.ToLower(), docData.Name);
            }


            //parse the number after the hyphen
            else if (word.Length > 1)
            {
                i++;
                termStr = handleHyphenDollar(word);
                addToDic(termStr.ToLower(), docData.Name);

            }
            else
                i++;
        }

        //set consecutive words that contain only capital letters as one term 
        private void handleCapitalLetters(DocumentData docData, ref string word, ref int i)
        {
            bool stop;
            bool enterWhille = false;
            string tmpS = word.ToLower();
            //add the first word to the dictionary if it not a stopword
            if (!stopWords.Contains(tmpS))
                addToDic(tmpS, docData.Name);
            i++;
            if (i < splited.Length)
            {
                word = splited[i];

                stop = false;
                //append all consecutive capital words 
                while (i < splited.Length && word.All(char.IsUpper) == true && stop == false)
                {
                    enterWhille = true;
                    word = toRemoveAny(word);
                    //turen the word to lowercase
                    word = word.ToLower();
                    if (!stopWords.Contains(word))
                        addToDic(word, docData.Name);
                    tmpS = tmpS + " " + word;
                    string tmp = splited[i];
                    if (i < splited.Length)
                    {
                        stop = toRemoveLast(tmp);
                    }
                    i++;
                    if (i < splited.Length)
                    {
                        word = splited[i];
                        word = toRemoveAny(word);
                    }

                }
                //add the new term to the dictionary
                if (enterWhille && !stopWords.Contains(tmpS))
                    addToDic(tmpS, docData.Name);
            }
        }

        //handle cases when the word is a number
        private string handleNumber(DocumentData docData, string word, ref int i, ref bool chopedLast)
        {
            string termStr;
            bool isDec = word.Contains('.');
            //check if the number is decimal
            if (isDec)
            {
                //check if the number bigger then million
                if (i < splited.Length - 1 && isBigAfter(ref i))
                {
                    termStr = parseBig(word, ref i);
                    i = i + 2;
                }
                else
                {
                    termStr = word;
                    i++;
                }
            }
            else
            {
                //check if the number is bigger then million                   
                if (i < splited.Length - 1 && isBigAfter(ref i))
                {
                    termStr = parseBig(word, ref i);
                    i = i + 2;
                }
                else
                {
                    termStr = parseNumber(word);
                    i++;
                }

            }

            //if the word is not end with punctuation or symbol
            if (!chopedLast)
            {
                if (i < splited.Length)
                {
                    if (isAfracWithAfter(splited[i]))
                    {
                        termStr += parseFrac(ref i);
                        //move to the next word
                        i++;
                    }

                    //check if the word conatain -
                    else if (splited[i].Contains('-'))
                    {
                        string tmpStr = (splited[i]);
                        while (tmpStr != "" && toRemoveLast(tmpStr))
                        {
                            chopedLast = true;
                            tmpStr = tmpStr.Substring(0, tmpStr.Length - 1);
                        }

                        string[] splitedStr = tmpStr.Split('-');
                        if (isAfrac(splitedStr[0]))
                        {
                            termStr += " " + handleHyphen(tmpStr, ref i, ref chopedLast).ToLower();
                        }
                    }
                }

                //theck if to continue to check the next string;
                if (!toRemoveLast(splited[i - 1]))
                {
                    checkafterNumber(ref i, ref termStr);

                }
            }
            //add the lowerCase of the term to the dictionary
            addToDic(termStr.ToLower(), docData.Name);
            return termStr;
        }




        private void addToDic(string str, string docName)
        {

            while (toRemoveLast(str))
                str = str.Substring(0, str.Length - 1);

            if (termsDic.ContainsKey(str))
            {
                termsDic[str] += 1;
            }
            else
            {
                termsDic.Add(str, 1);
                docInfo[docName].NumUniqueTerm++;


            }
            if (termsDic[str] > maxFrecInt)
            {
                maxFrecInt = termsDic[str];
            }

            parserList.Add(str);

        }

        #region privateFunctions

        //check if the number is part of price, percentege or distance in kilometers
        private void checkafterNumber(ref int i, ref string termStr)
        {
            string withoutEnd = splited[i];
            while (toRemoveLast(withoutEnd))
                withoutEnd = withoutEnd.Substring(0, withoutEnd.Length - 1);
            //check if there is dollar after the number
            if (i < splited.Length && withoutEnd == "Dollars")
            {
                termStr += " dollars";
                i++;
            }
            else if (i < splited.Length - 1 && splited[i] == "U.S.")
            {
                withoutEnd = splited[i + 1];
                while (toRemoveLast(withoutEnd))
                    withoutEnd = withoutEnd.Substring(0, withoutEnd.Length - 1);
                if (withoutEnd == "dollars" || withoutEnd == "Dollars")
                {
                    termStr += " dollars";
                    i = i + 2;
                }
            }
            //check if there is precent or precentage
            else if (i < splited.Length && (withoutEnd == "percent" || withoutEnd == "percentage"))
            {
                termStr += "%";
                i++;
            }

            //New rule - num KM -> numkm
            else if (i < splited.Length)
            {
                string lower = splited[i].ToLower();
                while (toRemoveLast(lower))
                    lower = lower.Substring(0, lower.Length - 1);

                if (lower == "km" || lower == "kilometer" || lower == "kilometres" || lower == "kilometers" || lower == "kms")
                {
                    termStr += "km";
                    i++;
                }
            }
        }

        //check if the word is part of a date

        private bool checkAndParseDate(ref int i, string docName)
        {

            string year = "";
            string month = "";
            string day = "";
            bool checkYear = true;
            string optDay;
            string optYear;
            if (i > splited.Length - 2)
                return false;

            //if the word not start with  2 digits or 2 digits and "th" after
            if (startWithTH(splited[i]) || isTwoDigit(splited[i]))
            {
                optDay = splited[i].Replace("th", "");
                if (!isDay(optDay))
                    return false;
                day = optDay;


                //check if the next word is a valid month
                string optMonth = splited[i + 1].ToLower();
                while (toRemoveLast(optMonth))
                {
                    optMonth = optMonth.Substring(0, optMonth.Length - 1);
                    checkYear = false;
                }
                if (!months.ContainsKey(optMonth))
                {
                    return false;
                }
                month = months[optMonth];

                //parse the day
                if (i <= splited.Length - 3 && checkYear)
                {
                    optYear = splited[i + 2];
                    while (toRemoveLast(optYear))
                        optYear = optYear.Substring(0, optYear.Length - 1);
                    year = isValidYear(optYear);
                    if (year == "")
                    {
                        addToDic(month + "-" + day, docName);
                        i += 2;
                        return true;

                    }
                    addToDic(year + "-" + month + "-" + day, docName);
                    i += 3;
                    return true;
                }
                if (month != "")
                {
                    addToDic(month + "-" + day, docName);
                    i += 2;
                    return true;
                }

            }

            string lowerMonth = splited[i].ToLower();
            if (!months.ContainsKey(lowerMonth))
                return false;
            month = months[lowerMonth];
            optDay = splited[i + 1];
            while (optDay != "" && toRemoveLast(optDay))
                optDay = optDay.Substring(0, optDay.Length - 1);
            //parse the day
            if (isDay(optDay))
            {
                int dayInt = int.Parse(optDay);
                if (dayInt < 10)
                    day = "0" + dayInt;
                else
                    day = "" + dayInt;
                if (i < splited.Length - 2 && splited[i + 1][splited[i + 1].Length - 1] == ',')
                {
                    optYear = splited[i + 2];
                    while (toRemoveLast(optYear))
                        optYear = optYear.Substring(0, optYear.Length - 1);
                    year = isValidYear(optYear);
                    //add the n
                    if (year == "")
                    {
                        addToDic(month + "-" + day, docName);
                        i += 2;
                        return true;
                    }
                    else
                    {
                        addToDic(year + "-" + month + "-" + day, docName);
                        i += 3;
                        return true;
                    }
                }
                else
                {
                    addToDic(month + "-" + day, docName);
                    i += 2;
                    return true;
                }

            }
            optYear = splited[i + 1];
            while (toRemoveLast(optYear))
                optYear = optYear.Substring(0, optYear.Length - 1);

            year = isValidYear(optYear);
            if (year != "")
            {
                addToDic(year + "-" + month, docName);
                i += 2;
                return true;
            }
            return false;
        }

        //check if the word contain only two digits
        private bool isTwoDigit(string str)
        {
            if (str.Length == 2 && str.All(Char.IsDigit))
                return true;
            return false;
        }


        //check if the word contain two numbers and "th"
        private bool startWithTH(string str)
        {

            if (str.Length == 4 && Char.IsDigit(str[0]) && Char.IsDigit(str[1]) && str[2] == 't' && str[3] == 'h')
                return true;
            return false;
        }

        //check if the string is a valid day
        private bool isDay(string optDay)
        {
            int res;
            bool ret = int.TryParse(optDay, out res);
            if (!ret)
                return false;
            if (res > 0 && res < 32)
            {
                return true;
            }
            return false;
        }

        //check if the string is valid year
        private string isValidYear(string optYear)
        {
            int numYear;
            if (optYear.Length != 4 && optYear.Length != 2)
                return "";
            bool isNum = int.TryParse(optYear, out numYear);
            if (!isNum)
                return "";
            if (numYear > 0 && numYear < 10)
            {
                return "190" + numYear;
            }
            else if (numYear > 9 && numYear < 21)
                return "20" + numYear;
            else if (numYear > 20 && numYear < 100)
                return "19" + numYear;
            else if (numYear > 1000 && numYear < 3000)
                return optYear;
            else
                return "";
        }

        //parse an string that represent a price with "-" or prices range
        private string handleHyphenDollar(string word)
        {
            string ans;
            string[] splitedDollar = word.Split('-');
            string firstNum = parseNumber(splitedDollar[0].Replace("$", ""));
            if (splitedDollar[1] == "million" || splitedDollar[1] == "Million" || splitedDollar[1] == "m")
            {

                if (firstNum == "")
                    return word;
                ans = "" + firstNum + " m dollars";

            }
            //check Billion
            else if (splitedDollar[1] == "billion" || splitedDollar[1] == "Billion" || splitedDollar[1] == "bn")
            {
                if (firstNum == "")
                    return word;
                ans = "" + firstNum + "000 m dollars";

            }
            //check Trillion
            else if (splitedDollar[1] == "trillion" || splitedDollar[1] == "Trillion")
            {

                if (firstNum == "")
                    return word;
                ans = "" + firstNum + "000000 m dollars";

            }

            //check if the word is not only prices
            else if (splitedDollar[1].All(char.IsLetter))
            {

                if (firstNum == "")
                    return word;
                ans = "" + firstNum + " dollars" + " " + splitedDollar[1];
            }
            else
            {
                if (firstNum == "")
                    return word;
                ans = firstNum + " ";

                //if the word is prices range remove the dollars symbols
                if (splitedDollar[1][0] == '$')
                {
                    ans = ans + "-" + parseNumber(splitedDollar[1].Replace("$", "")) + " dollars";

                }
                else
                {
                    string secondNum = parseNumber(splitedDollar[1]);
                    if (secondNum == "")
                        return word;
                    ans = "" + secondNum + " dollars";
                }


            }
            return ans;


        }


        //handle word that contains hyphens
        private string handleHyphen(string word, ref int i, ref bool chop)
        {
            string termStr = "";
            int j;
            string[] innerSplitting = word.Split('-');
            for (j = 0; j < innerSplitting.Length - 1; j++)
            {
                if (isNumber(innerSplitting[j]))
                {
                    termStr += parseNumber(innerSplitting[j]);
                    termStr += "-";
                }
                else
                {
                    termStr += innerSplitting[j];
                    termStr += "-";
                }
            }
            //if the word doesn't end with symbol chec if the number is bigger then million or fraction
            if (isNumber(innerSplitting[j]) && !chop)
            {

                if (isBigAfter(ref i))
                {
                    termStr += parseBig(innerSplitting[j], ref i);
                    i += 2;
                }

                else if (isAfracWithAfter(splited[i]))
                {
                    i++;
                    termStr += parseFrac(ref i);
                    i++;
                }
                else
                {
                    termStr += parseNumber(innerSplitting[j]);
                    i++;
                    if (isAfracWithAfter(splited[i]))
                    {
                        //i++;
                        termStr += parseFrac(ref i);
                        i++;
                    }
                }
            }
            else
            {
                termStr += innerSplitting[j];
                i++;
            }


            return termStr;
        }


        //check if the word doesn't tnd with a digit or a number or "%"
        private bool toRemoveLast(string word)
        {
            if (word.Length == 0)
                return false;
            // if (word[word.Length - 1] == ',' || word[word.Length - 1] == '.' || word[word.Length - 1] == ';' || (word[word.Length - 1] == ':'))
            if (word.Length > 0 && word[word.Length - 1] == '%')
                return false;
            if (char.IsDigit(word[word.Length - 1]) || char.IsLetter(word[word.Length - 1]))
                return false;
            return true;
        }

        //rempve unnececery symbols
        private string toRemoveAny(string word)
        {
            word = word.Replace("(", "");
            word = word.Replace(")", "");
            word = word.Replace("[", "");
            word = word.Replace("]", "");
            // word = word.Replace("--", "");
            word = word.Replace("|", "");
            word = word.Replace("?", "");
            word = word.Replace("#", "");
            word = word.Replace(":", "");
            word = word.Replace("`", "");
            word = word.Replace("!", "");
            word = word.Replace("^", "");
            word = word.Replace("`", "");
            word = word.Replace(";", "");
            word = word.Replace("~", "");
            if (word.Length != 0 && (word[word.Length - 1] == '.' || word[word.Length - 1] == ','))
                word = word.Substring(0, word.Length - 1);

            return word;
        }


        //parse a fraction
        public string parseFrac(ref int i)
        {
            string ans = "";
            string curStr = splited[i];
            if (!char.IsLetterOrDigit(curStr[curStr.Length - 1]))
                curStr = curStr.Substring(0, curStr.Length - 1);
            ans += " " + curStr;
            return ans;
        }

        //check if the next word  indicate that the number is bigger then 1 million
        private bool isBigAfter(ref int i)
        {

            if (i > splited.Length - 2)
                return false;
            string curStr = splited[i + 1];
            while (curStr != "" && toRemoveLast(curStr))
            {
                curStr = curStr.Substring(0, curStr.Length - 1);
            }

            if (curStr == "billion" || curStr == "Billion" || curStr == "bn" || curStr == "million" || curStr == "Million" || curStr == "m" || curStr == "trillion" || curStr == "Trillion")
                return true;
            return false;
        }


        //convert big number to million's unit
        public string parseBig(string curr, ref int i)
        {
            string ans = "";
            string num = curr;
            ////check last
            string big = splited[i + 1].ToLower();
            while (toRemoveLast(big))
            {
                big = big.Substring(0, big.Length - 1);
            }

            if (big == "million" || big == "m")
            {
                ans = num + " m";
            }
            else if (big == "billion" || big == "bn")
            {
                double numInt = double.Parse(num) * 1000;
                ans = "" + numInt + " m";
            }
            else if (big == "trillion")
            {
                double numInt = double.Parse(num) * 1000000;
                ans = "" + numInt + " m";
            }
            return ans;

        }

        //parse a number
        private string parseNumber(string numString)//, ref int i)
        {
            bool isNumber = false;
            double d = 0;
            string ans = numString;
            while (toRemoveLast(numString))
            {
                numString = numString.Substring(0, numString.Length - 1);
            }
            if (!isAfrac(numString))
            {
                string correctNum = numString.Replace(",", "");
                isNumber = double.TryParse(correctNum, out d);

                if (d >= 1000000)
                {
                    ans = d / 1000000 + " m";
                }
            }

            if (isNumber == false)
                return "";
            return ans;

        }


        //check if the word is a valid number
        private bool isNumber(string str)
        {
            if (str == "")
            {
                return false;
            }
            double result;
            if (toRemoveLast(str))
                return false;
            else
                str = str.Replace(",", "");
            return double.TryParse(str, out result);
        }

        //check if the word is valid frac
        private bool isAfrac(string str)
        {
            if (!("/".Any(str.Contains)))
            {
                return false;
            }
            string[] numbers = str.Split('/');
            int outInt;
            if (int.TryParse(numbers[0], out outInt) == false)
                return false;
            else
                return int.TryParse(numbers[0], out outInt);
        }

        //check if the word is a number or number with symbol after
        private bool isNumberWithAfter(string str)
        {
            if (str.EndsWith(".") || str.EndsWith(",") || str.EndsWith(";"))
            {
                str = str.Substring(0, str.Length - 1);
            }
            double result;
            return double.TryParse(str, out result);

        }

        //check if the word is a fraction or fraction with symbol after
        private bool isAfracWithAfter(string str)
        {

            while (toRemoveLast(str))
            {
                str = str.Substring(0, str.Length - 1);
            }
            if (str.Contains('-'))
                return false;


            if (!("/".Any(str.Contains)))
            {
                return false;
            }
            string[] numbers = str.Split('/');
            int outInt;
            if (int.TryParse(numbers[0], out outInt) == false)
                return false;
            else
                return int.TryParse(numbers[1], out outInt);
        }

        //chops thw unnececery firsts chars from the word
        private string chopStart(string s)
        {
            string ans = s;
            if (!Char.IsLetterOrDigit(ans[0]))
            {
                while (ans != "" && !Char.IsLetterOrDigit(ans[0]) && ans[0] != '$')
                {
                    ans = ans.Substring(1, ans.Length - 1);
                }
            }
            return ans;
        }

        //initialize the monthes dictionary
        private void addMonthes()
        {
            months.Add("january", "01");
            months.Add("february", "02");
            months.Add("march", "03");
            months.Add("april", "04");
            months.Add("may", "05");
            months.Add("june", "06");
            months.Add("july", "07");
            months.Add("august", "08");
            months.Add("september", "09");
            months.Add("october", "10");
            months.Add("november", "11");
            months.Add("december", "12");
            months.Add("jan", "01");
            months.Add("feb", "02");
            months.Add("mar", "03");
            months.Add("apr", "04");
            months.Add("jun", "06");
            months.Add("jul", "07");
            months.Add("aug", "08");
            months.Add("sep", "09");
            months.Add("oct", "10");
            months.Add("nov", "11");
            months.Add("dec", "12");
        }



    }

}

#endregion