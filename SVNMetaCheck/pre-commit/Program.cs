using System.Text.Json;
using BoysheO.Extensions;
using BoysheO.UnityEditor;
using SVNMetaCheck;


// const string reposURL = "http://127.0.0.1/svn/metaAvoidTest";
// const string user = "admin";
// const string pwd = "123";
const string template = "[exit:{0}][exec]{1}";
var repos = args[0];
var txn = args[1];

var appsettingJson = File.ReadAllText($"{repos}/hooks/appsetting.json");
var appsetting = JsonSerializer.Deserialize<AppSettingModel>(appsettingJson) ??
                 throw new Exception("deserialize json file fail");
string reposURL = appsetting.ReposURL;
string user = appsetting.User;
string pwd = appsetting.Pwd;

TimeSpan timeout = TimeSpan.FromSeconds(15);
var timeoutToken = new CancellationTokenSource(timeout);
var logger = new Logger();
logger.LogInformation("Hello World!");
logger.LogInformation(args.JoinAsOneString("\n"));

var cmd = $"svnlook changed -t \"{txn}\" \"{repos}\"";
// logger.LogError(cmd);
var (isSuccesss, processlog, code) = await CommandHelper.Invoke2Async(cmd);
if (!isSuccesss)
{
    throw new Exception(string.Format(template, code, cmd));
}

// logger.LogError(processlog);
var infos = SvnlookOutputResolver.Load(processlog);
var subSet = infos.Where(v => v.Action == ActionType.Update && v.Path.AsPath().GetExtension() == ".meta").ToArray();

await Parallel.ForEachAsync(subSet, timeoutToken.Token, async (entry, token) =>
{
    var getFileCommitingContent = $"svnlook cat -t \"{txn}\" \"{repos}\" \"{entry.Path}\"";
    // logger.LogError(getFileCommitingContent);
    var (isSuccesss2, fileCommitingContent, code2) =
        await CommandHelper.Invoke2Async(getFileCommitingContent, token: token);
    token.ThrowIfCancellationRequested();
    if (!isSuccesss2)
    {
        // logger.LogError(content);
        // logger.LogError($"get filecontent fault:{code2}");
        throw new Exception(string.Format(template, code2, getFileCommitingContent));
    }

    var guidCommiting = MetaGUIDResolver.GetGUID(fileCommitingContent);
    var path = $"{reposURL}/{entry.Path}";
    var getFileInRepos = $"svn cat \"{path}\" --non-interactive --username {user} --password {pwd}";
    // logger.LogError(getFileInRepos);
    await Task.Delay(TimeSpan.FromSeconds(1));
    var (isGetFileInReposOk, fileInReposContent, code3) =
        await CommandHelper.Invoke2Async(getFileInRepos, token: token);
    token.ThrowIfCancellationRequested();
    if (!isGetFileInReposOk)
    {
#if DEBUG
        throw new Exception(string.Format(template, code3, getFileInRepos));
#else
        throw new Exception(string.Format(template, code3,
            getFileInRepos.Replace(user, "**user**").Replace(pwd, "**pwd**")));
#endif
    }

    var guidExist = MetaGUIDResolver.GetGUID(fileInReposContent);
    // logger.LogError($"guid={guidCommiting} vs {guidExist}");
    if (guidCommiting != guidExist)
    {
        throw new Exception(
            $"commit is rejected:Not allow to modify guid in {entry.Path}.commitingGuid={guidCommiting},existGuid={guidExist}");
    }
});

// Console.Error.WriteLine("done");
return 0;