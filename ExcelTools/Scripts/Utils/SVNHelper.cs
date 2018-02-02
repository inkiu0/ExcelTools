﻿using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

public class SVNHelper
{
    public const string STATE_ADDED = "added";
    public const string STATE_DELETED = "deleted";
    public const string STATE_MODIFIED = "modified";
    public const string STATE_CONFLICT = "conflict";
    public const string STATE_LOCKED = "locked";

    /// <summary>
    /// arg0 = path
    /// arg1 2 3... = other arguments
    /// command = svn update arg0 otherargs
    /// </summary>
    /// <param name="args"></param>
    public static void Update(params string[] args)
    {
        string arguments = "update " + string.Join(" ", args);
        CommandHelper.ExcuteCommand("svn", arguments);
    }

    public static void Revert(params string[] args)
    {
        string arguments = "revert " + string.Join(" ", args);
        CommandHelper.ExcuteCommand("svn", arguments);
    }

    /// <summary>
    /// 锁定文件，一次只允许锁一个文件
    /// </summary>
    public static bool Lock(string path,string message = null)
    {
        string arguments = "lock -m " + "\"" + message + "\" " + path;
        string info = CommandHelper.ExcuteCommand("svn", arguments, true);
        //TODO:这里判断不知道是否严谨，待验证
        if (info.Contains("warning"))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public static void ReleaseLock(string path)
    {
        string arguments = "unlock " + path;
        CommandHelper.ExcuteCommand("svn", arguments);
    }

    public static void Commit(params string[] args)
    {
        string arguments = "commit " + string.Join(" ", args);
        CommandHelper.ExcuteCommand("svn", arguments);
    }

    /// <summary>
    /// 获取目标路径最新改动的版本号
    /// </summary>
    /// <param name="args">需包含本地路径或URL，本地路径获得本地Revision，URL获得服务器最新Revision</param>
    public static string GetLastestReversion(params string[] args)
    {
        string rev = "";
        string arguments = "info " + string.Join(" ", args);
        string info = CommandHelper.ExcuteCommand("svn", arguments, true);
        string[] infoArray = info.Split('\n', '\r');
        foreach (string str in infoArray)
        {
            if (str.StartsWith("Revision:"))
            {
                rev =str.Split(' ')[1];
            }
        }
        return rev;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="args">需包含版本号,目标路径</param>
    public static string Differ(params string[] args)
    {
        string arguments = "diff -r" + string.Join(" ", args);
        string output = CommandHelper.ExcuteCommand("svn", arguments, true);
        return null;
    }

    /// <summary>
    /// 获取服务器上指定文件的最新拷贝
    /// </summary>
    public static void CatFile(string fileUrl, string aimPath, bool setHidden)
    {
        string arguments = "/C svn cat " + fileUrl + " > " + aimPath;
        CommandHelper.ExcuteCommand("cmd", arguments);
        if (setHidden)
        {
            FileUtil.SetHidden(aimPath, true);
        }
    }

    /// <summary>
    /// 可获得目标文件的状态
    /// </summary>
    /// <param name="args">需包含目标文件的路径</param>
    public static Dictionary<string, string[]> Status(params string[] args)
    {
        string arguments = "status " + string.Join(" ", args);
        string output = CommandHelper.ExcuteCommand("svn", arguments, true);
        string[] statusArray = output.Split('\n', '\r');
        Dictionary<string, string[]> statusDic = new Dictionary<string, string[]>();
        foreach (string str in statusArray)
        {
            if (str != "")
            {
                string[] tmp = str.Split(' ');
                //info数组元素：[0]状态[1]锁定状态[2]文件路径
                string[] info = new string[3] { "/", "", "" };
                info[0] = tmp[0];
                info[2] = tmp[tmp.Length -1];
                for (int i = 1; i< tmp.Length -1;i++)
                {
                    if(tmp[i] != "")
                    {
                        info[1] = tmp[i];
                        break;
                    }
                }
                string path = info[2].Replace(@"\","/");
                string key;
                string state = IdentiToState(info[0]);
                string islockbyMe = IdentiToState(info[1]);
                //if (state[1] != "")
                //{
                //    val = IdentiToState(state[1]);
                //}
                if (info[0] != "" || info[1] != "")
                {
                    if (Directory.Exists(path))
                    {
                        List<string> files = FileUtil.CollectFolder(path, ".xlsx");
                        for (int i = 0; i < files.Count; i++)
                        {
                            key = files[i];
                            if (!statusDic.ContainsKey(key))
                            {
                                statusDic.Add(key, new string[2] { state, islockbyMe });
                            }
                        }
                    }
                    else
                    {
                        key = path;
                        if (!statusDic.ContainsKey(key))
                        {
                            statusDic.Add(key, new string[2] { state, islockbyMe });
                        }
                    }
                }
            }
        }
        return statusDic;
    }

    public static string LockInfo(string path)
    {
        string lockInfo = null;
        string arguments = "info " + PathToUrl(path);
        string info = CommandHelper.ExcuteCommand("svn", arguments, true);
        string[] infoArray = info.Split('\n', '\r');
        foreach (string str in infoArray)
        {
            if (str.StartsWith("Lock"))
            {
                lockInfo += str + "\n";
            }
        }
        return lockInfo;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path">只能填文件的本地路径</param>
    public static bool IsLockedByMe(string path)
    {
        string arguments = "info " + path;
        string info = CommandHelper.ExcuteCommand("svn", arguments, true);
        if(info.Contains("Lock Owner"))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //请求进入编辑状态
    //一、尝试请求锁定所需文件
    //二、当
    //    1、Excel
    //    2、Excel对应的各个分支的lua配置
    //    都由本机用户持有锁时，可以进入编辑状态
    //三、否则，显示加锁失败提示
    public static bool RequestEdit(string path)
    {
        bool CanBeEdit = false;
        if (IsLockedByMe(path))
        {
            CanBeEdit = true;
        }
        else if (Lock(path, "请求锁定" + path))
        {
            CanBeEdit = true;
        }
        else
        {
            string message = SVNHelper.LockInfo(path);
            string caption = "此文件锁定中";
            MessageBoxButtons buttons = MessageBoxButtons.OK;
            MessageBox.Show(message, caption, buttons);
        }

        return CanBeEdit;
    }

    private static string IdentiToState(string identifier)
    {
        switch (identifier)
        {
            case "!":
                return STATE_DELETED;
            case "?":
                return STATE_ADDED;
            case "M":
                return STATE_MODIFIED;
            case "C":
                return STATE_CONFLICT;
            case "K":
                return STATE_LOCKED;
            default:
                return identifier;
        }
    }

    public static string PathToUrl(string path)
    {
        string url = "svn://svn.sg.xindong.com/RO/client-trunk/" + path.Substring(path.IndexOf("Cehua"));
        return url;
    }
} 
