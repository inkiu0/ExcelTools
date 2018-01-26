using System.IO;
using System.Collections.Generic;
using System;
using System.Windows.Forms;
using System.Reflection;

class FileUtil
{
    public static List<string> CollectFolder(string folder, string ext)
    {
        List<string> files = new List<string>();
        if (Directory.Exists(folder))
            CollectFile(ref files, folder, new List<string>() { ext }, true);
        return files;
    }

    public static List<string> CollectFolderExceptExt(string folder, string ext)
    {
        List<string> files = new List<string>();
        if (Directory.Exists(folder))
            CollectFileExceptExts(ref files, folder, new List<string>() { ext }, true);
        return files;
    }

    public static List<string> CollectAllFolders(List<string> folders, string ext, Boolean collectHidden = false)
    {
        List<string> files = new List<string>();
        for (int i = 0; i < folders.Count; i++)
        {
            if (Directory.Exists(folders[i]))
                CollectFile(ref files, folders[i], new List<string>() { ext }, true, "", collectHidden);
        }
        return files;
    }

    public static List<string> CollectFolder(string folder, string ext, Action<string, string, string,string> match)
    {
        List<string> files = new List<string>();
        if (Directory.Exists(folder))
            CollectFile(ref files, folder, new List<string>() { ext }, true, "", false,match);
        return files;
    }

    public static List<string> CollectAllFolders(List<string> folders, List<string> exts)
    {
        List<string> files = new List<string>();
        for (int i = 0; i < folders.Count; i++)
        {
            if (Directory.Exists(folders[i]))
                CollectFile(ref files, folders[i], exts, true);
        }
        return files;
    }

    public static void CollectFile(ref List<string> fileList, string folder, List<string> exts, bool recursive = false, string ppath = "", Boolean collectHidden = false, Action<string, string, string, string> match = null)
    {
        folder = AppendSlash(folder);
        ppath = AppendSlash(ppath);
        DirectoryInfo dir = new DirectoryInfo(folder);
        FileInfo[] files = dir.GetFiles();
        for (int i = 0; i < files.Length; i++)
        {
            if (exts.Contains(files[i].Extension.ToLower()))//e.g ".txt"
            {
                string fpath = folder + files[i].Name;
                if (!string.IsNullOrEmpty(fpath))
                {
                    FileAttributes attributes = File.GetAttributes(fpath);
                    if(!( ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden) ^ collectHidden) ){
                        fileList.Add(fpath);
                        match?.Invoke(fpath, ppath, files[i].Name, null);
                    }
                }
            }
        }

        if (recursive)
        {
            foreach (var sub in dir.GetDirectories())
            {
                CollectFile(ref fileList, folder + sub.Name, exts, recursive, ppath + sub.Name, collectHidden, match);
            }
        }
    }

    public static void CollectFileExceptExts(ref List<string> fileList, string folder, List<string> exts, bool recursive = false, string ppath = "")
    {
        folder = AppendSlash(folder);
        ppath = AppendSlash(ppath);
        DirectoryInfo dir = new DirectoryInfo(folder);
        FileInfo[] files = dir.GetFiles();
        for (int i = 0; i < files.Length; i++)
        {
            if (!exts.Contains(files[i].Extension.ToLower()))//e.g ".txt"
            {
                string fpath = folder + files[i].Name;
                if (!string.IsNullOrEmpty(fpath))
                    fileList.Add(fpath);
            }
        }

        if (recursive)
        {
            foreach (var sub in dir.GetDirectories())
            {
                CollectFile(ref fileList, folder + sub.Name, exts, recursive, ppath + sub.Name);
            }
        }
    }

    public static bool RenameFile(string filePath, string rename)
    {
        try
        {
            if (File.Exists(filePath))
            {
                string aimPath = filePath.Remove(filePath.LastIndexOf('/') + 1) + rename;
                File.Move(filePath, aimPath);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch(IOException e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
    }

    public static void SetHidden(string path,Boolean doHidden = false)
    {
        if(doHidden)
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
        else
            File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.Hidden);
    }

    public static void DeleteHiddenFile(List<string> floders, string ext)
    {
        List<string> hiddenFiles = CollectAllFolders(floders, ext, true);
        for(int i = 0; i < hiddenFiles.Count; i++)
        {
            if (File.Exists(hiddenFiles[i]))
            {
                try
                {
                    File.Delete(hiddenFiles[i]);
                }
                catch(IOException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }

    public static void OpenFile(string path)
    {
        if (SVNHelper.IsLockedByMe(path))
        {
            OpenExcelApplication(path);
        }
        else if (SVNHelper.Lock(path, "请求锁定" + path))
        {
            OpenExcelApplication(path);
        }
        else
        {
            string message = SVNHelper.LockInfo(path);
            string caption = "此文件锁定中";
            MessageBoxButtons buttons = MessageBoxButtons.OK;
            MessageBox.Show(message, caption, buttons);
        }
    }

    public static string AppendSlash(string path)
    {
        if (path == null || path == "")
            return "";
        int idx = path.LastIndexOf('/');
        if (idx == -1)
            return path + "/";
        if (idx == path.Length - 1)
            return path;
        return path + "/";
    }

    public static void OverWriteText(string path, string contents)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        using (StreamWriter sw = File.CreateText(path))
        {
            sw.Write(contents);
        }
    }

    public static string PathCombine(params string[] paths)
    {
        var path = Path.Combine(paths);
        path = path.Replace(Path.DirectorySeparatorChar, '/');
        return path;
    }

    private static bool OpenExcelApplication(string path)
    {
        if (!File.Exists(path))
        {
            throw new Exception(path + "文件不存在！");
        }
        else
        {
            try
            {
                Microsoft.Office.Interop.Excel.Application excelApplication = new Microsoft.Office.Interop.Excel.Application();
                Microsoft.Office.Interop.Excel.Workbooks excelWorkbooks = excelApplication.Workbooks;
                Microsoft.Office.Interop.Excel.Workbook excelWorkbook = excelWorkbooks.Open(path, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value, Missing.Value) as Microsoft.Office.Interop.Excel.Workbook;
                excelApplication.Visible = true;

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("（1）程序中没有安装Excel程序。（2）或没有安装Excel所需要支持的.NetFramework\n详细信息：{0}", ex.Message));
            }
        }
    }

}
