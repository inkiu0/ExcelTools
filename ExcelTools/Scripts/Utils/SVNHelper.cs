
public class SVNHelper
{
    /// <summary>
    /// arg0 = path
    /// arg1 2 3... = other arguments
    /// command = svn update arg0 otherargs
    /// </summary>
    /// <param name="args"></param>
    public static void update(params string[] args)
    {
        string arguments = "update " + string.Join(" ", args);
        CommandHelper.ExcuteCommand("svn", arguments);
    }

    public static void revert(params string[] args)
    {

    }
} 
