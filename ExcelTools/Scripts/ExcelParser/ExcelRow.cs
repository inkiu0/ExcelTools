using NPOI.SS.UserModel;
using System.Collections.Generic;
using System.Text;

public class ExcelRow
{
    public int index;
    public List<ExcelCell> cells = new List<ExcelCell>();
    public Excel parent { get; private set; }
    public ExcelRow(int idx, Excel p)
    {
        index = idx;
        parent = p;
    }

    public void AppendCell(ExcelCell c)
    {
        cells.Add(c);
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        for(int i = 0; i < cells.Count; i++)
        {
            if(i == 0)
                sb.AppendFormat("[{0}] = {{", cells[i].GetValue());
            if (i != cells.Count - 1)
                sb.AppendFormat("{0}, ", cells[i].ToString());
            else
                sb.AppendFormat("{0}}}", cells[i].ToString());
        }
        return sb.ToString();
    }
}
