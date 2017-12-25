using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ExcelTools
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        CollectionViewSource view = new CollectionViewSource();
        ObservableCollection<ExcelFileListItem> _ExcelFiles = new ObservableCollection<ExcelFileListItem>();
        List<string> _Folders = new List<string>(){
            "D:/RO/ROTrunk/Cehua/Table/serverexcel",
            "D:/RO/ROTrunk/Cehua/Table/SubConfigs"
        };
        const string _Ext = ".xlsx";
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> files = FileUtil.CollectAllFolders(_Folders, _Ext);
            for (int i = 0; i < files.Count; i++)
            {
                _ExcelFiles.Add(new ExcelFileListItem()
                {
                    Name = Path.GetFileNameWithoutExtension(files[i]),
                    Status = "C/S",
                    ClientServer = "C/S",
                    FilePath = files[i]
                });
            }

            view.Source = _ExcelFiles;
            this.fileListView.DataContext = view;
            this.fileListView.MouseDoubleClick += FileListView_MouseDoubleClick;
        }

        private void FileListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListView listView = sender as ListView;
            ExcelFileListItem item = listView.SelectedItem as ExcelFileListItem;
            Excel excel = Excel.Parse(item.FilePath);
            string tmp = excel.ToString();
        }
    }
}