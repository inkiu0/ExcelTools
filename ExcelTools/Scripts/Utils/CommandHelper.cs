using System;
using System.Diagnostics;

public class CommandHelper
{
    public static void ExcuteCommand(string command, string argument)
    {
        ProcessStartInfo start = new ProcessStartInfo(command, argument);
        start.CreateNoWindow = true;
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.RedirectStandardInput = true;
        start.StandardOutputEncoding = System.Text.UTF8Encoding.UTF8;
        start.StandardErrorEncoding = System.Text.UTF8Encoding.UTF8;
        Process ps = new Process();
        ps.StartInfo = start;
        ps.Start();
        ps.WaitForExit();
        Console.WriteLine(ps.StandardError.ReadToEnd());
        Console.WriteLine(ps.StandardOutput.ReadToEnd());
        ps.Close();
    }

    public static void ExcuteCommandNoLog(string command, string argument)
    {
        Process ps = Process.Start(command, argument);
        ps.WaitForExit();
        ps.Close();
    }
}
