using System;
using NPOI.SS.UserModel;
using System.Collections.Generic;
using System.IO;
using NPOI.XSSF.UserModel;
using System.Text;

public class Excel
{
    public ISheet mainSheet;
    public List<ExcelRow> rows = new List<ExcelRow>();
    public bool isServerTable = false;
    private List<PropertyInfo> _Properties = new List<PropertyInfo>();
    private int _PropertyNums = -1;
    public string tableName { get; private set; };
    private int m_nPropertyNums
    {
        get
        {
            if(mainSheet == null)
            {
                Console.Error.WriteLine("mainSheet 为空！");
                return _PropertyNums;
            }
            if(_PropertyNums < 0)
                _PropertyNums = Math.Min(mainSheet.GetRow(0).LastCellNum, Math.Min(mainSheet.GetRow(1).LastCellNum, mainSheet.GetRow(2).LastCellNum));
            return _PropertyNums;
        }
    }

    public Excel(ISheet sheet)
    {
        mainSheet = sheet;
    }

    public static Excel Parse(string file)
    {
        ISheet sheet = GetMainSheet(file);
        if (sheet != null)
        {
            Excel excel = new Excel(sheet);
            excel.ParsePropertyInfos();
            excel.ParseExcelContents();
            excel.SetTableName(file);
            return excel;
        }
        return null;
    }

    public override string ToString()
    {
        //用+号拼接的字符串分开Add可以略微提升性能，几毫秒级别，为了可读性不做优化。
        List<string> strList = new List<string>();
        strList.Add(tableName + "= {\n");
        for (int i = 0; i < rows.Count; i++)
        {
            if (i == rows.Count - 1)
                strList.Add("\t" + rows[i].ToString() + "\n");
            else
                strList.Add("\t" + rows[i].ToString() + ",\n");
        }
        strList.Add("}\nreturn" + tableName);
        return string.Concat(strList.ToArray());
    }

    static ISheet GetMainSheet(string file)
    {
        FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
        IWorkbook workbook = new XSSFWorkbook(fileStream);
        ISheet sheet = workbook.GetSheetAt(0);
        return sheet;
    }

    private void SetTableName(string filePath)
    {
        string filename = Path.GetFileNameWithoutExtension(filePath);
        filename = filename.Replace("_", "");
        tableName = string.Format("Table_{0}", filename);
    }

    private void ParsePropertyInfos()
    {
        // 预先缓存头四行
        IRow _IsServerRow = mainSheet.GetRow(0);
        IRow _CnameRow = mainSheet.GetRow(1);
        IRow _EnameRow = mainSheet.GetRow(2);
        IRow _DataTypeRow = mainSheet.GetRow(3);
        for (int i = 0; i < m_nPropertyNums; i++)
        {
            _Properties.Add(new PropertyInfo(_IsServerRow.GetCell(i), _CnameRow.GetCell(i), _EnameRow.GetCell(i), _DataTypeRow.GetCell(i)));
        }
    }

    private void ParseExcelContents()
    {
        for (int i = 4; i < mainSheet.LastRowNum; i++)
        {
            IRow row = mainSheet.GetRow(i);
            if (row != null)
            {
                rows.Add(ParseExcelRow(row, i));
            }
        }
    }

    private ExcelRow ParseExcelRow(IRow r, int idx)
    {
        ExcelRow row = new ExcelRow(idx, this);
        for (int i = 0; i < m_nPropertyNums; i++)
        {
            if (i < r.LastCellNum)
            {
                ExcelCell c = ParseExcelCell(r.GetCell(i), _Properties[i], row);
                row.AppendCell(c);
            }
        }
        return row;
    }

    private ExcelCell ParseExcelCell(ICell c, PropertyInfo info, ExcelRow row)
    {
        string content = c == null ? string.Empty : c.ToString();
        ExcelCell cell = new ExcelCell(row, content, info);
        return cell;
    }
}
