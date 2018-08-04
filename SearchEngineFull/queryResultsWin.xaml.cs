using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SearchEngine
{
    /// <summary>
    /// Interaction logic for DictionaryWin.xaml
    /// </summary>
    public partial class queryResultsWin : Window
    {


        public queryResultsWin()
        {
            InitializeComponent();
        }

        public queryResultsWin(IEnumerable itemsSource, int numOfRes)
        {
            InitializeComponent();
            resLbl.Content = resLbl.Content + " " + numOfRes + " Relevant Documents";
            listBox.ItemsSource = itemsSource;

        }
    }
}
