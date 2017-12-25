using System.IO;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.Collections.Generic;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;

class ExcelParser
{
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
}
