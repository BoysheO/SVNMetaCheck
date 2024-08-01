using Microsoft.Extensions.Options;

namespace SVNMetaCheck.HandlerScript;

public class SizeChecker : ISVNChecker
{
    private readonly IOptions<AppSettingModel> _appSettingModel;
    private readonly SVNContext _svnContext;


    public SizeChecker(IOptions<AppSettingModel> appSettingModel, SVNContext svnContext)
    {
        _appSettingModel = appSettingModel;
        _svnContext = svnContext;
    }

    public bool Filter(SVNLogEntry entry)
    {
        return entry.Action == ActionType.Add;
    }

    public async ValueTask<Result> Check(SVNLogEntry entry, CancellationToken token)
    {
        var size = await _svnContext.GetSizeCommitting(entry, token);
        var size_mb = size / 1024 / 1024;
        if (size_mb > _appSettingModel.Value.SizeMBLimit)
        {
            var msg = _appSettingModel.Value.SizeMBLimitMsg.Replace("{Size}",_appSettingModel.Value.SizeMBLimit.ToString())
                .Replace("{File}", entry.Path);
            return new Result()
            {
                IsSuccess = false,
                ErrorTips = msg,
            };
        }

        return new Result()
        {
            IsSuccess = true
        };
    }
}