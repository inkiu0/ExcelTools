using Lua;
using System.Collections.Generic;
using System.IO;

namespace ExcelTools.Scripts
{
    class GlobalCfg
    {
        //表格的路径
        static public string SourcePath = null;
        //现处于管理中的分支
        static public List<string> BranchURLs = new List<string>()
        {
            "svn://svn.sg.xindong.com/RO/client-trunk",
            "svn://svn.sg.xindong.com/RO/client-branches/Studio",
            "svn://svn.sg.xindong.com/RO/client-branches/TF",
            "svn://svn.sg.xindong.com/RO/client-branches/Release"
        };
        static public string ClientTablePath = "/client-refactory/Develop/Assets/Resources/Script";
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

        //因为需要显示，四个分支都一起生成处理
        public List<lparser.table> GetlTable(string exlpath, bool reParse = false)
        {
            string tableName = Path.GetFileName(ExcelParserFileHelper.GetTargetLuaPath(exlpath,false));
            List<string> urls = Path2URLs(ExcelParserFileHelper.GetTargetLuaPath(exlpath, false));
            List<string> tmpFolders = GenTmpPath(tableName);
            for (int i = 0; i < urls.Count; i++)
            {
                SVNHelper.CatFile(urls[i], tmpFolders[i]);
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
            Directory.CreateDirectory(Path.Combine(SourcePath, "../TmpTable/Trunk/"));
            Directory.CreateDirectory(Path.Combine(SourcePath, "../TmpTable/Studio/"));
            Directory.CreateDirectory(Path.Combine(SourcePath, "../TmpTable/TF/"));
            Directory.CreateDirectory(Path.Combine(SourcePath, "../TmpTable/Release/"));
            List<string> tmpFolders = new List<string>
            {
                Path.Combine(SourcePath, "../TmpTable/Trunk/", tableName),
                Path.Combine(SourcePath, "../TmpTable/Studio/", tableName),
                Path.Combine(SourcePath, "../TmpTable/TF/", tableName),
                Path.Combine(SourcePath, "../TmpTable/Release/", tableName)
            };
            return tmpFolders;
        }
    }
}
