namespace SVNMetaCheck;

public class Logger
{
    public void LogInformation(string msg)
    {
        Console.WriteLine(msg);
    }

    public void LogError(string msg)
    {
        Console.Error.WriteLine(msg);
    }
}