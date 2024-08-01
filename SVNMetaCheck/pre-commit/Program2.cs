using System.Collections.Immutable;
using BoysheO.Extensions;
using BoysheO.UnityEditor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SVNMetaCheck.HandlerScript;
using YamlDotNet.Serialization;

namespace SVNMetaCheck;

public class Program2
{
    public static async Task<int> Main(string[] args, CancellationToken token)
    {
        var repos = args[0];
        var txn = args[1];

        var collection = new ServiceCollection();
        collection.AddSingleton<IOptions<AppSettingModel>>(v =>
        {
            var appsettingYaml = File.ReadAllText($"{repos}/hooks/appsetting.yaml");
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();
            var appsetting = deserializer.Deserialize<AppSettingModel>(appsettingYaml) ??
                             throw new Exception("deserialize json file fail");
            return Options.Create(appsetting);
        });

        collection.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(SimpleConsoleLogger<>)));
        collection.AddSingleton<SVNContext>(v =>
        {
            var appsetting = v.GetRequiredService<IOptions<AppSettingModel>>();
            var logger = v.GetRequiredService<ILogger<SVNContext>>();

            var cmd = $"svnlook changed -t \"{txn}\" \"{repos}\"";
            var task = MyCommandHelper.Exec(cmd, logger, token);
            var output = task.Result;
            var infos = SvnlookOutputResolver.Load(output);
            var context = new SVNContext(txn, repos, infos.ToImmutableArray(), logger, appsetting);
            return context;
        });

        collection.AddTransient<ISVNChecker, GUIDChangeChecker>();
        collection.AddTransient<ISVNChecker, SizeChecker>();
        var s = collection.BuildServiceProvider();

        #region Run

        var ctx = s.GetRequiredService<SVNContext>();
        var checkers = s.GetServices<ISVNChecker>().ToImmutableArray();
        // foreach (var svnLogEntry in ctx.Changed)
        try
        {
            await Parallel.ForEachAsync(ctx.Changed, token, async (svnLogEntry, token) =>
            {
                foreach (var svnChecker in checkers)
                {
                    if (!svnChecker.Filter(svnLogEntry)) continue;
                    var r = await svnChecker.Check(svnLogEntry, token);
                    if (!r.IsSuccess)
                    {
                        throw new Exception(r.ErrorTips);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            var logger = s.GetRequiredService<ILogger<Program2>>();
            logger.LogError(ex.Message);
            return -1;
        }

        #endregion


        // await Parallel.ForEachAsync(infos, token, async (v, token) => { s.GetServices() });

        return 0;
    }
}