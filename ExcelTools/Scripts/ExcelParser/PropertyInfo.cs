using NPOI.SS.UserModel;

public class PropertyInfo
{
    /// <summary>
    /// 第0行
    /// </summary>
    public bool isServerProperty;
    /// <summary>
    /// 第1行
    /// </summary>
    public string cname;
    /// <summary>
    /// 第2行
    /// </summary>
    public string ename;
    /// <summary>
    /// 第3行
    /// </summary>
    public string type;

    public PropertyInfo(ICell row0, ICell row1, ICell row2, ICell row3)
    {
        isServerProperty = row0.ToString() == "1";
        cname = row1.ToString();
        ename = row2.ToString();
        type = row3.ToString();
    }
}
