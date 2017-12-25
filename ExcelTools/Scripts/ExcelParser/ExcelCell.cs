using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ExcelCell
{
    private int index;
    private string content;
    private PropertyInfo propertyInfo;
    public ExcelRow parent { get; private set; }
    public ExcelCell(ExcelRow p, string con, PropertyInfo info)
    {
        parent = p;
        content = con;
        propertyInfo = info;
    }

    public override string ToString()
    {
        return propertyInfo.ename + " = " + getvalue();
        //return string.Format("{0} = {1}", propertyInfo.ename, getvalue());
    }

    private string getvalue()
    {
        //C#中拼接字符串，固定表达式a + b + c会被优化成string.Concat(new string[]{ a, b, c })
        //性能最好
        string tmp = string.Empty;
        switch (propertyInfo.type)
        {
            case "number":
                int n;
                float f;
                if (!string.IsNullOrEmpty(content) && int.TryParse(content, out n))
                    tmp = n.ToString();
                else if (content.IndexOf('.') > 0 && float.TryParse(content, out f))
                    tmp = f.ToString();
                else
                    tmp = "nil";
                break;
            case "string":
                tmp = content.Replace(@"\\", @"\\\\");
                tmp = tmp.Replace(@"\\\\n", @"\\n");
                tmp = "'" + tmp + "'";
                //tmp = string.Format("'{0}'", tmp);
                break;
            case "bittable":
                int num = 0;
                string[] bits = content.Split(',');
                int bit;
                for (int i = 0; i < bits.Length; i++)
                {
                    if(int.TryParse(bits[i].Trim(), out bit))
                        num += 1 << (bit - 1);
                }
                tmp = num.ToString();
                break;
            case "table":
                if (!parent.parent.isServerTable && string.IsNullOrEmpty(content))
                    tmp = "_EmptyTable";
                else
                    tmp = "{" + content + "}";
                    //tmp = string.Format("{{{0}}}", content);
                break;
            default:
                tmp = "nil";
                break;
        }
        return tmp;
    }
}
