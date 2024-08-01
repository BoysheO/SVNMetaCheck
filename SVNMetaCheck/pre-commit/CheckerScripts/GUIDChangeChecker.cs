using BoysheO.Extensions;

namespace SVNMetaCheck.HandlerScript;

public class GUIDChangeChecker:ISVNChecker
{
    private readonly SVNContext _svnContext;

    public GUIDChangeChecker(SVNContext svnContext)
    {
        _svnContext = svnContext;
    }

    public bool Filter(SVNLogEntry entry)
    {
        return entry.Action == ActionType.Update && entry.Path.AsPath().GetExtension() == ".meta";
    }

    public async ValueTask<Result> Check(SVNLogEntry entry, CancellationToken token)
    {
        var fileInReposContent = await _svnContext.GetContentTextInRepos(entry, token);
        var fileCommitingContent = await _svnContext.GetContentTextCommiting(entry, token);
        var guidExist = MetaGUIDResolver.GetGUID(fileInReposContent);
        var guidCommiting = MetaGUIDResolver.GetGUID(fileCommitingContent);
        if (guidCommiting != guidExist)
        {
            return new Result()
            {
                IsSuccess = false,
                ErrorTips =
                    $"commit is rejected:Not allow to modify guid in {entry.Path}.commitingGuid={guidCommiting},existGuid={guidExist}",
            };
        }

        return new Result()
        {
            IsSuccess = true
        };
    }
}