
using System.Text.RegularExpressions;

public static class MetaGUIDResolver
{
    private static readonly Regex _regex = new Regex("(?<=guid:\\s*)[a-z0-9]{32}", RegexOptions.Compiled);
    
    public static string GetGUID(string content)
    {
        //找第二行，guid：到行尾的内容
        var firstMatch = _regex.Match(content);
        return firstMatch.Value;
    }
}