using System.IO;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.Collections.Generic;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;

class ExcelParser
{
    string target_temp_table_path = "luas";
    string target_server_table_path = "../Lua/Table";
    string target_client_table_path = "../../client-refactory/Develop/Assets/Resources/Script/Config";
    string target_client_other_path = "../../client-refactory/Develop/Assets/Resources/Script/MConfig";
    string target_client_other_path_old = "../../client-refactory/Develop/Assets/Resources/Script/FrameWork/Config";
    string target_client_script_path = "../../client-refactory/Develop/Assets/Resources/Script/";
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

    }

    private void svn_update(string path, params string[] args)
    {
        SVNHelper.update(path);
    }
}
