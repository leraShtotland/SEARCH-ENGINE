using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IR
{
    class Documents
    {
        string _docNum;
        string _date;
        string _title;
        string _text;

        public string DocNum
        {
            get
            {
                return _docNum;
            }

            set
            {
                _docNum = value;
            }
        }

        public string Date
        {
            get
            {
                return _date;
            }

            set
            {
                _date = value;
            }
        }

        public string Title
        {
            get
            {
                return _title;
            }

            set
            {
                _title = value;
            }
        }

        public string Text
        {
            get
            {
                return _text;
            }

            set
            {
                _text = value;
            }
        }
    }
}
