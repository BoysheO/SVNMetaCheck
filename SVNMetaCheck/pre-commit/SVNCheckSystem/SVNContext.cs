using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SVNMetaCheck.HandlerScript;

public sealed class SVNContext
{
    public SVNContext(string txn, string repos, ImmutableArray<SVNLogEntry> changed, ILogger<SVNContext> logger,
        IOptions<AppSettingModel> appsetting)
    {
        this.txn = txn;
        this.repos = repos;
        Changed = changed;
        _logger = logger;
        Appsetting = appsetting;
    }

    public ImmutableArray<SVNLogEntry> Changed { get; }

    private readonly Dictionary<SVNLogEntry, long> _entry2SizeCommitting = new();
    private Dictionary<SVNLogEntry, string> _entry2Text = new();
    private Dictionary<SVNLogEntry, string> _entry2TextCommiting = new();
    private readonly string txn;
    private readonly string repos;
    private readonly ILogger<SVNContext> _logger;
    public IOptions<AppSettingModel> Appsetting { get; }

    public async ValueTask<long> GetSizeCommitting(SVNLogEntry entry, CancellationToken token)
    {
        if (_entry2SizeCommitting.TryGetValue(entry, out var size))
        {
            return size;
        }

        var cc = $"svnlook filesize -t {txn} \"{repos}\" \"{entry.Path}\"";
        var output = await MyCommandHelper.Exec(cc, _logger, token);
        var size_byte = long.Parse(output);
        lock (_entry2SizeCommitting)
        {
            _entry2SizeCommitting[entry] = size_byte;
        }

        return size_byte;
    }

    public async ValueTask<string> GetContentTextInRepos(SVNLogEntry entry, CancellationToken token)
    {
        if (_entry2Text.TryGetValue(entry, out var text))
        {
            return text;
        }

        var cc = $"svnlook cat \"{repos}\" \"{entry.Path}\"";
        var output = await MyCommandHelper.Exec(cc, _logger, token);
        lock (_entry2Text)
        {
            _entry2Text[entry] = output;
        }

        return output;
    }

    public async ValueTask<string> GetContentTextCommiting(SVNLogEntry entry, CancellationToken token)
    {
        if (_entry2TextCommiting.TryGetValue(entry, out var text))
        {
            return text;
        }

        var cc = $"svnlook cat -t \"{txn}\" \"{repos}\" \"{entry.Path}\"";
        var output = await MyCommandHelper.Exec(cc, _logger, token);
        lock (_entry2TextCommiting)
        {
            _entry2TextCommiting[entry] = output;
        }

        return output;
    }
}