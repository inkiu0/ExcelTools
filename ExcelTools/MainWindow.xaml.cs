using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using ExcelTools.Scripts.Utils;
using ExcelTools.Scripts;
using ExcelTools.Scripts.UI;

namespace ExcelTools
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string STATE_UPDATE = "Update";
        private const string STATE_REVERT = "Revert";
        private const string STATE_EDIT = "编辑";

        private string _localRev;
        private string _serverRev;

        private ExcelFileListItem _listItemChoosed;

        public MainWindow()
        {
            InitializeComponent();
        }

        CollectionViewSource view = new CollectionViewSource();
        ObservableCollection<ExcelFileListItem> _ExcelFiles = new ObservableCollection<ExcelFileListItem>();
        Dictionary<string, DifferController> _DiffDic = new Dictionary<string, DifferController>();
        const string _ConfigPath = "config.txt";
        List<string> _Folders = new List<string>(){
            "/serverexcel",
            "/SubConfigs"
        };
        List<string> _URLs = new List<string>()
        {
            "svn://svn.sg.xindong.com/RO/client-trunk/Cehua/Table/serverexcel",
            "svn://svn.sg.xindong.com/RO/client-trunk/Cehua/Table/SubConfigs"
        };
        const string _FolderServerExvel = "/serverexcel";
        const string _FolderSubConfigs = "/SubConfigs";
        const string _Ext = ".xlsx";
        const string _TempRename = "_server";

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();
            Refresh();
            this.fileListView.DataContext = view;
            this.fileListView.MouseDoubleClick += FileListView_MouseDoubleClick;
            GetRevision();
        }

        private void Refresh()
        {
            _Folders[0] = GlobalCfg._SourcePath + _FolderServerExvel;
            _Folders[1] = GlobalCfg._SourcePath + _FolderSubConfigs;
            _ExcelFiles.Clear();
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
            CheckStateBtn_Click(null, null);
        }

        private void LoadConfig()
        {
            if (!File.Exists(_ConfigPath))
            {
                ChooseSourcePath();
            }
            using (StreamReader cfgSt = new StreamReader(_ConfigPath))
            {    
                GlobalCfg._SourcePath = cfgSt.ReadLine();
                cfgSt.Close();    
            }        
        }

        private void ChooseSourcePath()
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowser.Description = "选择Table位置：";
            folderBrowser.ShowDialog();
            string path = folderBrowser.SelectedPath.Replace(@"\", "/");
            if (path.Contains("Table"))
            {
                using (StreamWriter cfgSt = new StreamWriter(_ConfigPath))
                {
                    cfgSt.WriteLine(path);
                    cfgSt.Close();
                }
            }
        }

        private void ChangeSourcePath_Click(object sender, RoutedEventArgs e)
        {
            ChooseSourcePath();
            LoadConfig();
            Refresh();
        }

        private void FileListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ListView listView = sender as ListView;
            ExcelFileListItem item = listView.SelectedItem as ExcelFileListItem;
            //Excel excel = Excel.Parse(item.FilePath, false);
            //string tmp = excel.ToString();
            //ExcelParser.ParseAll();
            _listItemChoosed = item;
            if (item == null)
            {
                return;
            }
            JudgeMultiFuncBtnState();
            if (item.Status == SVNHelper.STATE_MODIFIED)
            {
                ExcelParser.ParseTemp(item.FilePath);
                DifferController differController = _DiffDic[item.FilePath];
                if (differController.DiffItems.Count > 0)
                {
                    this.diffChooseBox.Text = differController.SelectedText;
                    this.diffChooseBox.ItemsSource = differController.DiffItems;
                }
            }
            else
            {
                this.diffChooseBox.Text = item.Status;
                this.diffChooseBox.ItemsSource = null;
            }
        }

        private void CheckStateBtn_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> statusDic = SVNHelper.Status(_Folders[0], _Folders[1]);
            _DiffDic.Clear();
            for(int i = 0; i < _ExcelFiles.Count; i++)
            {
                if (statusDic.ContainsKey(_ExcelFiles[i].FilePath))
                {
                    _ExcelFiles[i].Status = statusDic[_ExcelFiles[i].FilePath];
                    if(_ExcelFiles[i].Status == SVNHelper.STATE_MODIFIED)
                    {
                        string fileUrl = (_ExcelFiles[i].FilePath.Contains("serverexcel") ? _URLs[0] : _URLs[1]) + "/" + _ExcelFiles[i].Name + _Ext;
                        string aimPath = (_ExcelFiles[i].FilePath.Contains("serverexcel") ? _Folders[0] : _Folders[1]) + "/" + _ExcelFiles[i].Name + _TempRename + _Ext;
                        SVNHelper.CatFile(fileUrl, aimPath);
                    }
                }
                else
                {
                    _ExcelFiles[i].Status = "C/S";
                }
            }
            foreach (KeyValuePair<string, string> kv in statusDic)
            {
                //插入deleted文件
                if (kv.Value == SVNHelper.STATE_DELETED)
                {
                    _ExcelFiles.Add(new ExcelFileListItem()
                    {
                        Name = Path.GetFileNameWithoutExtension(kv.Key),
                        Status = kv.Value,
                        ClientServer = "C/S",
                        FilePath = kv.Key
                    });
                }
                //初始化modified文件的比较器
                if (kv.Value == SVNHelper.STATE_MODIFIED)
                {
                    string tmpExlPath = kv.Key.Remove(kv.Key.LastIndexOf('/') + 1) + Path.GetFileNameWithoutExtension(kv.Key) + _TempRename + _Ext;
                    _DiffDic[kv.Key] = new DifferController(kv.Key, tmpExlPath);
                    _DiffDic[kv.Key].Differ();
                }
            }
        }

        private void MultiFuncBtn_Click(object sender, RoutedEventArgs e)
        {
            Button senderBtn = sender as Button;
            switch (senderBtn.Content)
            {
                case STATE_UPDATE:
                    SVNHelper.Update(_Folders[0], _Folders[1]);
                    GetRevision();
                    break;
                case STATE_REVERT:
                    SVNHelper.Revert(_listItemChoosed.FilePath);
                    CheckStateBtn_Click(null, null);
                    break;
                case STATE_EDIT:
                    //FileUtil.OpenFile(_listItemChoosed.FilePath);
                    break;
                default:
                    break;
            }
        }

        private void GetRevision()
        {
            _localRev = SVNHelper.GetLastestReversion(_Folders[0]);
            _serverRev = SVNHelper.GetLastestReversion(_URLs[0]);
            JudgeMultiFuncBtnState();
        }

        private void JudgeMultiFuncBtnState()
        {
            multiFuncBtn.Visibility = Visibility.Visible;
            //需要Update
            if (_localRev != _serverRev)
            {
                multiFuncBtn.Content = STATE_UPDATE;
            }
            //有modified
            else if (_listItemChoosed != null && _listItemChoosed.Status == SVNHelper.STATE_MODIFIED)
            {
                multiFuncBtn.Content = STATE_REVERT;
            }
            else if (_listItemChoosed != null && _listItemChoosed.Status == "C/S")
            {
                multiFuncBtn.Content = STATE_EDIT;
            }
            else
            {
                multiFuncBtn.Visibility = Visibility.Hidden;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            //do my stuff before closing
            FileUtil.DeleteHiddenFile(_Folders, _Ext);
            base.OnClosing(e);
        }
    }
}