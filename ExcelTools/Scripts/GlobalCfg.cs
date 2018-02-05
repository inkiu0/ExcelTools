using ExcelTools.Scripts.UI;
using ExcelTools.Scripts.Utils;
using Lua;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using static ExcelTools.Scripts.Utils.DifferController;
using static Lua.lparser;

namespace ExcelTools.Scripts
{
    class GlobalCfg
    {
        //表格的路径
        static public string SourcePath = null;
        static public string LocalTmpTablePath = "../TmpTable/Local/";
        //现处于管理中的分支
        static public List<string> BranchURLs = new List<string>()
        {
            "svn://svn.sg.xindong.com/RO/client-trunk",
            "svn://svn.sg.xindong.com/RO/client-branches/Studio",
            "svn://svn.sg.xindong.com/RO/client-branches/TF",
            "svn://svn.sg.xindong.com/RO/client-branches/Release"
        };
        static public List<string> TmpTablePaths = new List<string>()
        {
            "../TmpTable/Trunk/",
            "../TmpTable/Studio/",
            "../TmpTable/TF/",
            "../TmpTable/Release/"
        };
        static public string ClientTablePath = "/client-refactory/Develop/Assets/Resources/Script";

        static public int BranchCount { get { return BranchURLs.Count; } }

        static private string _Local_Table_Ext = ".txt";
        private static GlobalCfg _instance;

        public static GlobalCfg Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GlobalCfg();
                return _instance;
            }
        }

        private GlobalCfg()
        {
            _ExcelDic = new Dictionary<string, Excel>();
            _lTableDic = new Dictionary<string, List<lparser.table>>();
        }

        //所有表格的解析都存在这里
        //TODO：之后可能用多线程提早个别表格的解析操作，优化操作体验
        private Dictionary<string, Excel> _ExcelDic;

        //Table配置的解析存在这里
        private Dictionary<string, List<lparser.table>> _lTableDic;

        public Excel GetParsedExcel(string path, bool reParse = false)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            if (!_ExcelDic.ContainsKey(path) || reParse)
            {
                _ExcelDic[path] = Excel.Parse(path, false);
            }
            return _ExcelDic[path];
        }

        private List<table> GetLuaTables(string excelpath)
        {
            //对比md5，看是否需要重新生成LocalLuaTable llt
            string tablename = string.Format("Table_{0}", Path.GetFileNameWithoutExtension(excelpath));
            string lltpath = Path.Combine(SourcePath, LocalTmpTablePath, tablename + ".txt");
            string md5 = ExcelParserFileHelper.GetMD5HashFromFile(excelpath);
            if(!File.Exists(lltpath) || md5 != lparser.ReadTableMD5(lltpath))
                ExcelParser.ReGenLuaTable(excelpath);
            List<table> ts = new List<table>();
            ts.Add(lparser.parse(lltpath));
            List<string> branchs = GenTmpPath(tablename);
            for (int i = 0; i < branchs.Count; i++)
            {
                if (File.Exists(branchs[i]))
                    ts.Add(lparser.parse(branchs[i]));
                else
                    ts.Add(null);
            }
            return ts;
        }

        #region UI数据相关
        List<table> currentTables = new List<table>();
        List<tablediff> currentTablediffs = new List<tablediff>();

        private List<string> GetRowAllStatus(string rowid)
        {
            List<string> status = new List<string>();
            for (int i = 0; i < BranchURLs.Count; i++)
            {
                if (currentTablediffs.Count > i && currentTablediffs[i] != null)
                {
                    if (currentTablediffs[i].addedrows.ContainsKey(rowid))
                        status.Add(DifferController.STATUS_ADDED);
                    else if (currentTablediffs[i].deletedrows.ContainsKey(rowid))
                        status.Add(DifferController.STATUS_DELETED);
                    else if (currentTablediffs[i].modifiedrows.ContainsKey(rowid))
                        status.Add(DifferController.STATUS_MODIFIED);
                    else
                        status.Add(DifferController.STATUS_NONE);
                }
                else
                    status.Add(DifferController.STATUS_DELETED);
            }
            return status;
        }

        private Dictionary<string, IDListItem> GetExcelDeletedRow()
        {
            Dictionary<string, IDListItem> tmpDic = new Dictionary<string, IDListItem>();
            for (int i = 0; i < currentTablediffs.Count; i++)
            {
                if (currentTablediffs[i] != null)
                {
                    foreach (var id in currentTablediffs[i].deletedrows.Keys)
                    {
                        if (!tmpDic.ContainsKey(id))
                        {
                            tmpDic.Add(id, new IDListItem
                            {
                                ID = id,
                                Row = -1,
                                States = new List<string>()
                            });
                            for (int k = 0; k < BranchCount; k++)//初始化状态为STATUS_NONE
                                tmpDic[id].States.Add(DifferController.STATUS_NONE);
                        }
                        tmpDic[id].States[i] = DifferController.STATUS_DELETED;
                    }
                }
            }
            return tmpDic;
        }

        public ObservableCollection<IDListItem> GetIDList(string excelpath)
        {
            currentTables = GetLuaTables(excelpath);
            currentTablediffs.Clear();
            for (int i = 1; i < currentTables.Count; i++)
            {
                //if (currentTables[i] != null)
                    currentTablediffs.Add(DifferController.CompareTable(currentTables[0], currentTables[i]));
                //else
                //    currentTablediffs.Add(null);
            }
            ObservableCollection<IDListItem> idlist = new ObservableCollection<IDListItem>();
            for(int i = 0; i < currentTables[0].configs.Count; i++)
            {
                idlist.Add(new IDListItem
                {
                    ID = currentTables[0].configs[i].key,
                    Row = i,
                    States = GetRowAllStatus(currentTables[0].configs[i].key)
                });
            }
            Dictionary<string, IDListItem> tmpDic = GetExcelDeletedRow();
            foreach (var item in tmpDic.Values)
                idlist.Add(item);
            return idlist;
        }

        public void ApplyRow(int branchIdx, IDListItem item)
        {
            //if(currentTablediffs.Count > branchIdx && currentTables.Count > branchIdx + 1 &&
            //    currentTablediffs[branchIdx] != null && currentTables[branchIdx + 1] != null &&
            //    item != null && item.States.Count > branchIdx)
            //{
            //}
            table lt = currentTables[0];//local table
            table bt = currentTables[branchIdx + 1];//branch table
            tablediff btd = currentTablediffs[branchIdx];//branch tablediff

            if (bt == null)
                bt = new table(lt);

            string status = item.States[branchIdx];
            btd.Apply(status, item.ID);

            if (status == DifferController.STATUS_ADDED)
                bt.Apply(status, null, item.ID);
            else if (lt.configsDic.ContainsKey(item.ID))
            {
                config cfg = lt.configsDic[item.ID];
                bt.Apply(status, cfg);
            }
            string tmp = bt.GenString(null, btd);
        }
        #endregion

        //因为需要显示，四个分支都一起生成处理
        public List<lparser.table> GetlTable(string exlpath, bool reParse = false)
        {
            //这里之后表格位置修改需要处理一下的，GetTargetLuaPath不好用
            string tableName = Path.GetFileName(ExcelParserFileHelper.GetTargetLuaPath(exlpath,false));
            List<string> urls = Path2URLs(ExcelParserFileHelper.GetTargetLuaPath(exlpath, false));
            List<string> tmpFolders = GenTmpPath(tableName);
            for (int i = 0; i < urls.Count; i++)
            {
                SVNHelper.CatFile(urls[i], tmpFolders[i], false);
            }
            if (!_lTableDic.ContainsKey(exlpath) || reParse)
            {
                _lTableDic[exlpath] = new List<lparser.table>();
                for (int i = 0; i < tmpFolders.Count; i++)
                {
                    _lTableDic[exlpath].Add(lparser.parse(tmpFolders[0]));
                }
            }
            return _lTableDic[exlpath];
        }

        public List<lparser.config> GetTableRow(string id)
        {
            List<lparser.config> rows = new List<config>();
            for (int i = 0; i < currentTables.Count; i++)
            {
                if (currentTables[i] != null && currentTables[i].configsDic.ContainsKey(id))
                    rows.Add(currentTables[i].configsDic[id]);
                else
                    rows.Add(null);
            }
            return rows;
        }

        static private List<string> Path2URLs(string tablePath)
        {
            string str = tablePath.Substring(tablePath.LastIndexOf("/Config"));
            List<string> urlList = new List<string>();
            for (int i = 0; i < BranchURLs.Count; i++)
            {
                string url = BranchURLs[i] + ClientTablePath + str;
                urlList.Add(url);
            }
            return urlList;
        }

        //生成临时table的路径
        private static List<string> GenTmpPath(string tableName)
        {
            Directory.CreateDirectory(Path.Combine(SourcePath, TmpTablePaths[0]));
            Directory.CreateDirectory(Path.Combine(SourcePath, TmpTablePaths[1]));
            Directory.CreateDirectory(Path.Combine(SourcePath, TmpTablePaths[2]));
            Directory.CreateDirectory(Path.Combine(SourcePath, TmpTablePaths[3]));
            List<string> tmpFolders = new List<string>
            {
                Path.Combine(SourcePath, TmpTablePaths[0], tableName) + _Local_Table_Ext,
                Path.Combine(SourcePath, TmpTablePaths[1], tableName) + _Local_Table_Ext,
                Path.Combine(SourcePath, TmpTablePaths[2], tableName) + _Local_Table_Ext,
                Path.Combine(SourcePath, TmpTablePaths[3], tableName) + _Local_Table_Ext
            };
            return tmpFolders;
        }
    }
}
