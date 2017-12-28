using System.IO;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.Collections.Generic;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;

class ExcelParser
{
    static List<string> _Folders = new List<string>(){
            "D:/RO/ROTrunk/Cehua/Table/serverexcel",
            "D:/RO/ROTrunk/Cehua/Table/SubConfigs"
    };
    static string _Ext = ".xlsx";
    static string _ClientExt = ".txt";
    static string _ServerExt = ".lua";
    static string source_path = "D:/RO/ROTrunk/Cehua/Table/luas";
    static string target_server_table_path = "../Lua/Table";
    static string target_client_table_path = "../../client-refactory/Develop/Assets/Resources/Script/Config";

    public ExcelParser(string file)
    {
        Application app = new Application();
        _Workbook wb = app.Workbooks.Add(file);
        List<Worksheet> sheets = new List<Worksheet>();
        for (int i = 0; i < wb.Sheets.Count; i++)
        {
            sheets.Add(wb.Sheets[i] as Worksheet);
        }
    }

    public static IWorkbook Parse(string file)
    {
        FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
        IWorkbook workbook = new XSSFWorkbook(fileStream);
        ISheet mainSheet = workbook.GetSheetAt(0);
        return workbook;
    }

    public static void ParseAll()
    {
        SVNHelper.update(source_path);
        List<string> files = FileUtil.CollectFolder(source_path, _Ext, MatchExcelFile);
        for (int i = 0; i < files.Count; i++)
        {
            //_ExcelFiles.Add(new ExcelFileListItem()
            //{
            //    Name = Path.GetFileNameWithoutExtension(files[i]),
            //    Status = "C/S",
            //    ClientServer = "C/S",
            //    FilePath = files[i]
            //});
        }
    }

    private static void GenServerVersion(Excel excel, string relativeDir, string fileNameContainExt)
    {
        string fname = excel.tableName + _ServerExt;
        string tmp = excel.ToString();
        using (StreamWriter sw = File.CreateText(Path.Combine(source_path, target_server_table_path, fname)))
        {
            sw.Write(tmp);
        }
    }

    private static void GenClientVersion(Excel excel, string relativeDir, string fileNameContainExt)
    {
        string fname = excel.tableName + _ClientExt;
        string tmp = excel.ToString();
        using (StreamWriter sw = File.CreateText(Path.Combine(source_path, target_client_table_path, relativeDir, fname)))
        {
            sw.Write(tmp);
        }
    }

    private static void MatchExcelFile(string path, string relativeDir, string fileNameContainExt)
    {
        Excel excel = Excel.Parse(path);
        if (relativeDir.IndexOf("serverexcel") < 0)
            GenClientVersion(excel, relativeDir, fileNameContainExt);
        GenServerVersion(excel, relativeDir, fileNameContainExt);
    }
}
