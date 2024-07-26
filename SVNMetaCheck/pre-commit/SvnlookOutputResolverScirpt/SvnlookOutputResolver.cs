using System.Collections.Concurrent;

namespace SVNMetaCheck;

public class SvnlookOutputResolver
{
    public static SVNLogEntry[] Load(string log)
    {
        var sp = log.TrimEnd().Split("\n");
        var entries = new SVNLogEntry[sp.Length];
        Parallel.For(0, sp.Length, i =>
        {
            var log = sp[i].Trim();
            ActionType actionType;
            switch (log[0])
            {
                case 'U':
                    actionType = ActionType.Update;
                    break;
                //在本例中只要检查update条目
                default:
                    actionType = ActionType.Unknown;
                    break;
            }

            var path = log.Substring(1).Trim();
            entries[i] = new SVNLogEntry()
            {
                Action = actionType,
                Path = path,
            };
        });
        return entries;
    }
}