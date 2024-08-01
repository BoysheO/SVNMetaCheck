using System.Reactive.Disposables;
using Microsoft.Extensions.Logging;

namespace SVNMetaCheck;

public class SimpleConsoleLogger<T>:ILogger<T>
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var text = GetText();
        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Information:
            case LogLevel.Warning:
            case LogLevel.None:
                Console.WriteLine(text);
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                Console.Error.WriteLine(text);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }

        string GetText()
        {
            return formatter(state, exception);
        }
    }
    

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return Disposable.Empty;
    }
}