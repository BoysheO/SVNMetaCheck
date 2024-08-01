using BoysheO.UnityEditor;
using Microsoft.Extensions.Logging;

namespace SVNMetaCheck.HandlerScript;

public static class MyCommandHelper
{
    public static async ValueTask<string> Exec(string cmd, ILogger? logger, CancellationToken token)
    {
        var (isSuccesss, processlog, code) = await CommandHelper.Invoke2Async(cmd, token: token);
        if (!isSuccesss)
        {
            logger?.LogError("[exit:{0}][exec]{1}", code, processlog);
            throw new Exception("exec fail.");
        }

        return processlog;
    }
}