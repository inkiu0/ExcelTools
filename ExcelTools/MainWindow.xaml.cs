﻿using System;
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
        const string _TempRename = "_tmp";

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();
            tableListView.DataContext = view;
            tableListView.SelectionChanged += FileListView_SelectionChange;
            idListView.SelectionChanged += IDListView_SelectChange;
            idListView.MouseRightButtonDown += IDListView_RightClick;
            tableListView.Items.SortDescriptions.Add(new SortDescription("Status", ListSortDirection.Descending));
            tableListView.Items.IsLiveSorting = true;
            GetRevision();
        }

        //1. 加载配置
        //1.1 设置源路径
        //1.2 加载所有文件
        #region 加载配置
        private void LoadConfig(bool force = false)
        {
            SetSourcePath(force);
            LoadFiles();
        }

        private void LoadFiles()
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
                    LockByMe = "",
                    ClientServer = "C/S",
                    FilePath = files[i]
                });
            }
            view.Source = _ExcelFiles;
            //CheckStateBtn_Click(null, null);
        }

        private void SetSourcePath(bool force)
        {
            if (force || !File.Exists(_ConfigPath))
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
                Description = "选择Cehua/Table位置：",
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
            }
            else
            {
                string message = path + "不是Cehua/Table路径\n请选择包含Table的路径！";
                string caption = "Error";
                System.Windows.Forms.MessageBoxButtons buttons = System.Windows.Forms.MessageBoxButtons.OK;
                System.Windows.Forms.MessageBox.Show(message, caption, buttons);
            }
        }
        #endregion

        private void ChangeSourcePath_Click(object sender, RoutedEventArgs e)
        {
            LoadConfig(true);
        }

        private void FileListView_SelectionChange(object sender, SelectionChangedEventArgs e)
        {
            ListView listView = sender as ListView;
            ExcelFileListItem item = listView.SelectedItem as ExcelFileListItem;
            _listItemChoosed = item;
            if (item == null)
            {
                return;
            }
            JudgeMultiFuncBtnState();
            idListView.ItemsSource = null;
            propertyListView.ItemsSource = null;
            idListView.ItemsSource = GlobalCfg.Instance.GetIDList(item.FilePath);
        }

        private void IDListView_RightClick(object sender, MouseButtonEventArgs e)
        {
            //TODO:取消此行修改
            ListView listView = sender as ListView;
            IDListItem item = listView.SelectedItem as IDListItem;
            int row = item.Row;
            _DiffDic[_listItemChoosed.FilePath].RevertModified(row);
            idListView.ItemsSource = _DiffDic[_listItemChoosed.FilePath].IDListItems;
        }

        private void IDListView_SelectChange(object sender, SelectionChangedEventArgs e)
        {
            IDListItem item = (sender as ListView).SelectedItem as IDListItem;
            if(item == null)
                return;

            Excel excel = GlobalCfg.Instance.GetParsedExcel(_listItemChoosed.FilePath);
            List<PropertyInfo> propertyList = excel.Properties;
            ObservableCollection<PropertyListItem> fieldList = new ObservableCollection<PropertyListItem>();

            List<lparser.config> configs = GlobalCfg.Instance.GetTableRow(item.ID);
            string ename = string.Empty;
            for (int i = 0; i < propertyList.Count; i++)
            {
                ename = propertyList[i].ename;
                fieldList.Add(new PropertyListItem()
                {
                    PropertyName = propertyList[i].cname,
                    Context = configs[0] != null && configs[0].propertiesDic.ContainsKey(ename) ? configs[0].propertiesDic[ename].value : null,
                    Trunk = configs[1] != null && configs[1].propertiesDic.ContainsKey(ename) ? configs[1].propertiesDic[ename].value : null,
                    Studio = configs[2] != null && configs[2].propertiesDic.ContainsKey(ename) ? configs[2].propertiesDic[ename].value : null,
                    TF = configs[3] != null && configs[3].propertiesDic.ContainsKey(ename) ? configs[3].propertiesDic[ename].value : null,
                    Release = configs[4] != null && configs[4].propertiesDic.ContainsKey(ename) ? configs[4].propertiesDic[ename].value : null
                });
            }
            propertyListView.ItemsSource = fieldList;
        }

        private void CheckStateBtn_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string[]> statusDic = SVNHelper.Status(_Folders[0], _Folders[1]);
            for(int i = 0; i < _ExcelFiles.Count; i++)
            {
                if (statusDic.ContainsKey(_ExcelFiles[i].FilePath))
                {
                    _ExcelFiles[i].Status = statusDic[_ExcelFiles[i].FilePath][0];
                    _ExcelFiles[i].LockByMe = statusDic[_ExcelFiles[i].FilePath][1];
                }
                else
                {
                    _ExcelFiles[i].Status = "/";
                    _ExcelFiles[i].LockByMe = "/";
                }
            }
            #region 插入deleted文件(已注释)
            //foreach (KeyValuePair<string, string[]> kv in statusDic)
            //{
            //    if (kv.Value[0] == SVNHelper.STATE_DELETED)
            //    {
            //        _ExcelFiles.Add(new ExcelFileListItem()
            //        {
            //            Name = Path.GetFileNameWithoutExtension(kv.Key),
            //            Status = kv.Value[0],
            //            LockByMe = kv.Value[1],
            //            ClientServer = "C/S",
            //            FilePath = kv.Key
            //        });
            //    }
            //}
            #endregion
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
                    if (_listItemChoosed.Status == SVNHelper.STATE_MODIFIED)
                    {
                        SVNHelper.Revert(_listItemChoosed.FilePath);
                    }
                    if(_listItemChoosed.Status == SVNHelper.STATE_ADDED)
                    {
                        File.Delete(_listItemChoosed.FilePath);
                        _ExcelFiles.Remove(_listItemChoosed);
                    }
                    CheckStateBtn_Click(null, null);
                    break;
                case STATE_EDIT:
                    //请求进入编辑状态
                    if (SVNHelper.RequestEdit(_listItemChoosed.FilePath))
                    {
                        _listItemChoosed.IsEditing = true;
                    }
                    else
                    {
                        _listItemChoosed.IsEditing = false;
                    }
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
            multiFunctionBtn.Visibility = Visibility.Visible;
            //需要Update
            if (_localRev != _serverRev)
            {
                multiFunctionBtn.Content = STATE_UPDATE;
            }
            //和SVN版本库中有差异(MODIFIED和ADDED)
            else if (_listItemChoosed != null && 
                (_listItemChoosed.Status == SVNHelper.STATE_MODIFIED || _listItemChoosed.Status == SVNHelper.STATE_ADDED))
            {
                multiFunctionBtn.Content = STATE_REVERT;
            }
            //可请求进入编辑状态
            else if (_listItemChoosed != null && _listItemChoosed.Status == "/")
            {
                multiFunctionBtn.Content = STATE_EDIT;
            }
            else
            {
                multiFunctionBtn.Visibility = Visibility.Hidden;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            //do my stuff before closing
            FileUtil.DeleteHiddenFile(new List<string> { GlobalCfg.SourcePath + "/.." }, _Ext);
            FileUtil.DeleteHiddenFile(new List<string> { GlobalCfg.SourcePath + "/.." }, ".txt");
            base.OnClosing(e);
        }

        private void GenTableBtn_Click(object sender, RoutedEventArgs e)
        {
            Button genBtn = sender as Button;
            string aimUrl = "";
            string tmpPath = "";
            switch (genBtn.Name)
            {
                case "genTableBtn_Release":
                    aimUrl += GlobalCfg.BranchURLs[3] + GlobalCfg.ClientTablePath;
                    tmpPath += GlobalCfg.SourcePath + "/" + GlobalCfg.TmpTablePaths[3];
                    break;
                case "genTableBtn_TF":
                    aimUrl += GlobalCfg.BranchURLs[2] + GlobalCfg.ClientTablePath;
                    tmpPath += GlobalCfg.SourcePath + "/" + GlobalCfg.TmpTablePaths[2];
                    break;
                case "genTableBtn_Studio":
                    aimUrl += GlobalCfg.BranchURLs[1] + GlobalCfg.ClientTablePath;
                    tmpPath += GlobalCfg.SourcePath + "/" + GlobalCfg.TmpTablePaths[1];
                    break;
                case "genTableBtn_Trunk":
                    aimUrl += GlobalCfg.BranchURLs[0] + GlobalCfg.ClientTablePath;
                    tmpPath += GlobalCfg.SourcePath + "/" + GlobalCfg.TmpTablePaths[0];
                    break;
            }
            _DiffDic[_listItemChoosed.FilePath].ConfirmChangesAndCommit(tmpPath, aimUrl);
        }
    }
}