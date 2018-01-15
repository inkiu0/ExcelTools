using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelTools.Scripts.UI
{
    public class DiffItem: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int Row { get; set;}

        public string State { get; set; }

        public string Context { get; set; }

    }
}
