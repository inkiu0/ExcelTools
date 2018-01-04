using System.IO;
using System.Collections.Generic;
using System;

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

    public static List<string> CollectAllFolders(List<string> folders, string ext)
    {
        List<string> files = new List<string>();
        for (int i = 0; i < folders.Count; i++)
        {
            if (Directory.Exists(folders[i]))
                CollectFile(ref files, folders[i], new List<string>() { ext }, true);
        }
        return files;
    }

    public static List<string> CollectFolder(string folder, string ext, Action<string, string, string> match)
    {
        List<string> files = new List<string>();
        if (Directory.Exists(folder))
            CollectFile(ref files, folder, new List<string>() { ext }, true, "", match);
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

    public static void CollectFile(ref List<string> fileList, string folder, List<string> exts, bool recursive = false, string ppath = "", Action<string, string, string> match = null)
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
                    fileList.Add(fpath);
                    match?.Invoke(fpath, ppath, files[i].Name);
                }
            }
        }

        if (recursive)
        {
            foreach (var sub in dir.GetDirectories())
            {
                CollectFile(ref fileList, folder + sub.Name, exts, recursive, ppath + sub.Name, match);
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

    public static string PathCombine(params string[] paths)
    {
        var path = Path.Combine(paths);
        path = path.Replace(Path.DirectorySeparatorChar, '/');
        return path;
    }
}
