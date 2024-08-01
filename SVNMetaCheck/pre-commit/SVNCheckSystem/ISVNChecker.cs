using YamlDotNet.Core.Tokens;

namespace SVNMetaCheck.HandlerScript;

public interface ISVNChecker
{
    bool Filter(SVNLogEntry entry);
    ValueTask<Result> Check(SVNLogEntry entry, CancellationToken token);
}