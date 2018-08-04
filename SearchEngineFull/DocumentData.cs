using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    public class DocumentData
    {
        public string Name { get; set; }
        public int max_tf { get; set; }
        public int NumUniqueTerm { get; set; }
        public string Language { get; set; }
        public string Date { get; set; }
        public int Length { get; set; }

        public DocumentData()
        {
            Name = "";
            max_tf = 0;
            NumUniqueTerm = 0;
            Language = "";
            Date = "";
            Length = 0; 

        }


    }
}
