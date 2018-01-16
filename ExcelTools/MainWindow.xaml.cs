using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ExcelTools.Scripts.Utils;
using ExcelTools.Scripts;
using System.ComponentModel;

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
        const string _URL = "svn://svn.sg.xindong.com/RO/client-trunk";
        const string _FolderServerExvel = "/serverexcel";
        const string _FolderSubConfigs = "/SubConfigs";
        const string _Ext = ".xlsx";
        const string _TempRename = "_server";

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();
            Refresh();
            fileListView.DataContext = view;
            fileListView.MouseDoubleClick += FileListView_MouseDoubleClick;
            fileListView.Items.SortDescriptions.Add(new SortDescription("Status", ListSortDirection.Descending));
            fileListView.Items.IsLiveSorting = true;
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
                    Status = "/",
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
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择Table位置：",
                ShowNewFolderButton = false,
            };
            folderBrowser.ShowDialog();
            string path = folderBrowser.SelectedPath.Replace(@"\", "/");
            if (path.Contains("Table"))
            {
                using (StreamWriter cfgSt = new StreamWriter(_ConfigPath))
                {
                    cfgSt.WriteLine(path);
                    cfgSt.Close();
                }
                LoadConfig();
                Refresh();
            }
            else
            {
                string message = path + "不是Table的路径\n请选择包含Table的路径！";
                string caption = "Error";
                System.Windows.Forms.MessageBoxButtons buttons = System.Windows.Forms.MessageBoxButtons.OK;
                System.Windows.Forms.MessageBox.Show(message, caption, buttons);
            }
        }

        private void ChangeSourcePath_Click(object sender, RoutedEventArgs e)
        {
            ChooseSourcePath();
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
                //ExcelParser.ParseTemp(item.FilePath);
                string tmpExlPath = item.FilePath.Remove(item.FilePath.LastIndexOf('/') + 1) + Path.GetFileNameWithoutExtension(item.FilePath) + _TempRename + _Ext;
                _DiffDic[item.FilePath] = new DifferController(item.FilePath, tmpExlPath);
                _DiffDic[item.FilePath].Differ();
                if (_DiffDic[item.FilePath].DiffItems.Count > 0)
                {
                    DifferWindow differWindow = new DifferWindow(item.Name, _DiffDic[item.FilePath].DiffItems);
                    differWindow.Show();
                }
            }
            else
            {
                //TODO:提示 并没有内容修改，不需要重新生成配置，建议revert
            }
        }

        private void CheckStateBtn_Click(object sender, RoutedEventArgs e)
        {
            ExcelParser.ParseAll();
            Dictionary<string, string> statusDic = SVNHelper.Status(_Folders[0], _Folders[1]);
            for(int i = 0; i < _ExcelFiles.Count; i++)
            {
                if (statusDic.ContainsKey(_ExcelFiles[i].FilePath))
                {
                    _ExcelFiles[i].Status = statusDic[_ExcelFiles[i].FilePath];
                    if(_ExcelFiles[i].Status == SVNHelper.STATE_MODIFIED)
                    {
                        string fileUrl = _URL + "/" + _ExcelFiles[i].FilePath.Substring(_ExcelFiles[i].FilePath.IndexOf("Cehua"));
                        string aimPath = _ExcelFiles[i].FilePath.Insert(_ExcelFiles[i].FilePath.LastIndexOf("."), _TempRename);
                        SVNHelper.CatFile(fileUrl, aimPath);
                    }
                }
                else
                {
                    _ExcelFiles[i].Status = "/";
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
            _serverRev = SVNHelper.GetLastestReversion(_URL);
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
            else if (_listItemChoosed != null && _listItemChoosed.Status == "/")
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