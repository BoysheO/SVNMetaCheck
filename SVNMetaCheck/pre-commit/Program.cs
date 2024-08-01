using System.Text.Json;
using BoysheO.Extensions;
using BoysheO.UnityEditor;
using BoysheO.Util;
using SVNMetaCheck;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

return await Program2.Main(args,default);

// const string reposURL = "http://127.0.0.1/svn/metaAvoidTest";
// const string user = "admin";
// const string pwd = "123";
const string template = "[exit:{0}][exec]{1}";
var repos = args[0];
var txn = args[1];

var appsettingYaml = File.ReadAllText($"{repos}/hooks/appsetting.yaml");
var deserializer = new DeserializerBuilder()
    .IgnoreUnmatchedProperties()
    // .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .Build();
var appsetting = deserializer.Deserialize<AppSettingModel>(appsettingYaml) ??
                 throw new Exception("deserialize json file fail");
// string reposURL = appsetting.ReposURL;
// string user = appsetting.User;
// string pwd = appsetting.Pwd;

TimeSpan timeout = TimeSpan.FromSeconds(15);
var timeoutToken = new CancellationTokenSource(timeout);
var logger = new Logger();
// logger.LogInformation("Hello World!");
// logger.LogError(user);
// logger.LogError(pwd);

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
        logger.LogError(fileCommitingContent);
        // logger.LogError($"get filecontent fault:{code2}");
        throw new Exception(string.Format(template, code2, getFileCommitingContent));
    }

    var guidCommiting = MetaGUIDResolver.GetGUID(fileCommitingContent);
    // var path = $"{reposURL}/{entry.Path}";
    // var getFileInRepos = $"svn cat \"{path}\" --non-interactive --username {user} --password {pwd}";
    var getFileInRepos = $"svnlook cat \"{repos}\" \"{entry.Path}\"";
    // logger.LogError(getFileInRepos);
    var (isGetFileInReposOk, fileInReposContent, code3) =
        await CommandHelper.Invoke2Async(getFileInRepos, token: token);
    // logger.LogError(fileInReposContent);
    token.ThrowIfCancellationRequested();
    if (!isGetFileInReposOk)
    {
        // logger.LogError(fileInReposContent);
// #if DEBUG
        throw new Exception(string.Format(template, code3, getFileInRepos));
// #else
        // throw new Exception(string.Format(template, code3,
            // getFileInRepos.Replace(user, "**user**").Replace(pwd, "**pwd**")));
// #endif
    }

    var guidExist = MetaGUIDResolver.GetGUID(fileInReposContent);
    // logger.LogError($"guid={guidCommiting} vs {guidExist}");
    if (guidCommiting != guidExist)
    {
        throw new Exception(
            $"commit is rejected:Not allow to modify guid in {entry.Path}.commitingGuid={guidCommiting},existGuid={guidExist}");
    }

    var cc = $"svnlook filesize -t {txn} \"{repos}\" \"{entry.Path}\"";
    var sizeResult = await CommandHelper.Invoke2Async(cc, token: token);
    if (!sizeResult.isSuccesss)
    {
        logger.LogError(sizeResult.processlog);
        throw new Exception(string.Format(template, sizeResult.code, cc));
    }

    //文件大小检查
    {
        var size_byte = long.Parse(sizeResult.processlog);
        var size_mb = size_byte / 1024 / 1024;
        if (size_mb > appsetting.SizeMBLimit)
        {
            throw new Exception(appsetting.SizeMBLimitMsg);
        }
    }
    
});

// Console.Error.WriteLine("done");
return 0;