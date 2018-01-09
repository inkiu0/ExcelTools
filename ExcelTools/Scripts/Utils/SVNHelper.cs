﻿using System;
using System.Collections.Generic;
using System.IO;

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
    public static string Lock(string path,string message = null)
    {
        string arguments = "lock -m " + "\"" + message + "\" " + path;
        string info = CommandHelper.ExcuteCommand("svn", arguments, true);
        return info;
    }

    public static void UnLock(string path)
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
    public static void CatFile(string fileUrl, string aimPath)
    {
        string arguments = "/C svn cat " + fileUrl + " > " + aimPath;
        CommandHelper.ExcuteCommand("cmd", arguments);
        FileUtil.SetHidden(aimPath, true);
    }

    /// <summary>
    /// 可获得目标文件的状态
    /// </summary>
    /// <param name="args">需包含目标文件的路径</param>
    public static Dictionary<string, string> Status(params string[] args)
    {
        string arguments = "status " + string.Join(" ", args);
        string output = CommandHelper.ExcuteCommand("svn", arguments, true);
        string[] statusArray = output.Split('\n', '\r');
        Dictionary<string, string> statusDic = new Dictionary<string, string>();
        foreach (string str in statusArray)
        {
            if (str != "")
            {
                string[] tmp = str.Split(' ');
                string path = tmp[tmp.Length - 1].Replace(@"\","/");
                string key;
                string val = IdentiToState(tmp[0]);
                if (Directory.Exists(path))
                {
                    List<string> files = FileUtil.CollectFolder(path, ".xlsx");
                    for(int i=0; i < files.Count; i++)
                    {
                        key = files[i];
                        if (!statusDic.ContainsKey(key))
                        {
                            statusDic.Add(key, val);
                        }
                    }
                }
                else if (File.Exists(path))
                {
                    key = path;
                    if (!statusDic.ContainsKey(key))
                    {
                        statusDic.Add(key, val);
                    }
                }
            }
        }
        return statusDic;
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
                return null;
        }
    }
} 
