using System.Collections.Generic;
using System.IO;

public class ExcelParserFileHelper
{
    static List<string> donot_copy_files = new List<string>() { "Table_Activity.txt", "Table_ActivityStep.txt", "Table_Timer.txt", "Table_Laboratory.txt", "Table_StrayCats.txt", "Table_SuperAI.txt", "Table_SystemChat.txt", "Table_SealMonster.txt", "Table_Org.txt", "Table_OperateReward.txt", "Table_MonsterSkill.txt", "Table_MonsterEvolution.txt", "Table_MonsterEmoji.txt", "Table_MapSky.txt", "Table_MapHuntTreasure.txt" };
    static List<string> donot_delete_files = new List<string>() { "MenuUnclock" };

    string target_temp_table_path = "luas";
    string target_server_table_path = "../Lua/Table";
    static string target_client_table_path = "../../client-refactory/Develop/Assets/Resources/Script/Config";
    static string target_client_other_path = "../../client-refactory/Develop/Assets/Resources/Script/MConfig";
    string target_client_other_path_old = "../../client-refactory/Develop/Assets/Resources/Script/FrameWork/Config";
    string target_client_script_path = "../../client-refactory/Develop/Assets/Resources/Script/";

    public static void RemakePath(string path)
    {
        if (Directory.Exists(path))
        {
            RemoveAllFileExceptMeta(path);
        }
        else if (File.Exists(path))
        {
            //pass
        }
        else
            Directory.CreateDirectory(path);
    }

    public static bool isDoNotCopyFile(string fname)
    {
        return donot_copy_files.IndexOf(fname) > -1;
    }

    public static string GenTargetFilePath(string path)
    {
        return Path.Combine(target_client_table_path, path);
    }

    private static void RemoveAllFileExceptMeta(string root)
    {
        List<string> files = FileUtil.CollectFolderExceptExt(root, ".meta");
        for (int i = 0; i < files.Count; i++)
            File.Delete(files[i]);
    }
}
