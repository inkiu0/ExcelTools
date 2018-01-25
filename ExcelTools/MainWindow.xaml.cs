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
using System.Windows.Input;
using ExcelTools.Scripts.UI;
using Lua;

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
            tabelListView.DataContext = view;
            tabelListView.MouseDoubleClick += FileListView_MouseDoubleClick;
            idListView.MouseDoubleClick += IDListView_MouseDoubleClick;
            tabelListView.Items.SortDescriptions.Add(new SortDescription("Status", ListSortDirection.Descending));
            tabelListView.Items.IsLiveSorting = true;
            GetRevision();
        }

        private void Refresh()
        {
            _Folders[0] = GlobalCfg.SourcePath + _FolderServerExvel;
            _Folders[1] = GlobalCfg.SourcePath + _FolderSubConfigs;
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
                GlobalCfg.SourcePath = cfgSt.ReadLine();
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
            idListView.ItemsSource = null;
            propertyListView.ItemsSource = null;
            branchListView.ItemsSource = null;
            if (item.Status == SVNHelper.STATE_MODIFIED)
            {
                //ExcelParser.ParseTemp(item.FilePath);
                string tmpExlPath = item.FilePath.Remove(item.FilePath.LastIndexOf('/') + 1) + Path.GetFileNameWithoutExtension(item.FilePath) + _TempRename + _Ext;
                _DiffDic[item.FilePath] = new DifferController(item.FilePath, tmpExlPath);
                _DiffDic[item.FilePath].Differ();
                if (_DiffDic[item.FilePath].IDListItems.Count > 0)
                {
                    idListView.ItemsSource = _DiffDic[item.FilePath].IDListItems;
                }
            }
            else
            {
                //TODO:提示 并没有内容修改，不需要重新生成配置，建议revert
            }
        }

        private void IDListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListView listView = sender as ListView;
            IDListItem item = listView.SelectedItem as IDListItem;
            Excel excel = GlobalCfg.Instance.GetParsedExcel(_listItemChoosed.FilePath);
            List<PropertyInfo> propertyList = excel.Properties;
            ObservableCollection<PropertyListItem> fieldList = new ObservableCollection<PropertyListItem>();
            ObservableCollection<BranchListItem> branchList = new ObservableCollection<BranchListItem>();

            int trunkCfgIndex = 0;
            int studioCfgIndex = 0;
            int tfCfgIndex = 0;
            int releaseCfgIndex = 0;
            List<lparser.table> tables = GlobalCfg.Instance.GetlTable(_listItemChoosed.FilePath);
            for (int i = 0; i < tables[0].configs.Count; i++)
            {
                if (tables[0].configs[i].key == item.ID.ToString())
                {
                    trunkCfgIndex = i;
                }
            }
            for (int i = 0; i < tables[1].configs.Count; i++)
            {
                if (tables[0].configs[i].key == item.ID.ToString())
                {
                    studioCfgIndex = i;
                }
            }
            for (int i = 0; i < tables[2].configs.Count; i++)
            {
                if (tables[0].configs[i].key == item.ID.ToString())
                {
                    tfCfgIndex = i;
                }
            }
            for (int i = 0; i < tables[3].configs.Count; i++)
            {
                if (tables[0].configs[i].key == item.ID.ToString())
                {
                    releaseCfgIndex = i;
                }
            }

            for (int i = 0; i < propertyList.Count; i++)
            {
                fieldList.Add(new PropertyListItem()
                {
                    PropertyName = propertyList[i].cname,
                    Context = excel.rows[item.Row - 5].cells[i].GetValue()
                });
            }
            for(int i = 0; i < tables[0].configs[trunkCfgIndex].properties.Count; i++)
            {
                branchList.Add(new BranchListItem()
                {
                    Trunk = tables[0].configs[trunkCfgIndex].properties[i].value,
                    Studio = tables[1].configs[studioCfgIndex].properties[i].value,
                    TF = tables[2].configs[tfCfgIndex].properties[i].value,
                    Release = tables[3].configs[releaseCfgIndex].properties[i].value
                });
            }
            propertyListView.ItemsSource = fieldList;
            branchListView.ItemsSource = branchList;
        }

        private void CheckStateBtn_Click(object sender, RoutedEventArgs e)
        {
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