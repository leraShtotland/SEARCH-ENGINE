using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    public class TermData
    {
        // in which posting file the term saved
        public string Path { get; set; }
        //in which line in the posting file the term saved
        public int LineNum { get; set; }
        // count the number of docs that have this term
        public int Df { get; set; }
        //count the number of the term in the corpus
        public int NumInCorpus { get; set; } 
    }
}
