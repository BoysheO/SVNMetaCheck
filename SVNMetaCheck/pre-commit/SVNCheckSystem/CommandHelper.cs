using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoysheO.UnityEditor
{
    public static class CommandHelper
    {
        public static async Task<(bool isSuccesss, string processlog, int code)> Invoke2Async(string cmd,
            bool printLog = true, CancellationToken token = default)
        {
            if (token.IsCancellationRequested) return (false, "", -1);
            var id = new System.Random().Next();
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var head = $"[id:{id}][thread:{threadId}]";
            var (fileName, argument) = GetProcessCommand(cmd);
            Console.WriteLine($"{head}exec:{fileName} {argument}");
            using var proc = System.Diagnostics.Process.Start(new ProcessStartInfo()
            {
                Arguments = argument,
                FileName = fileName,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
            }) ?? throw new Exception("creat process fail");
            // ReSharper disable once AccessToDisposedClosure
            using var cancelToken = token.Register(() => proc.Kill());
            if (printLog)
            {
                Console.WriteLine($"<color=red>{head}-------------Start read standard output--------------</color>");
            }

            using var tk2 = new CancellationTokenSource();
            using var tk3 = CancellationTokenSource.CreateLinkedTokenSource(tk2.Token, token);

            var sb = new StringBuilder();
            var t1 = Task.Run(async () =>
            {
                //开始读取
                using var sr = proc.StandardOutput;
                while (!sr.EndOfStream)
                {
                    tk3.Token.ThrowIfCancellationRequested();
                    var line = await sr.ReadLineAsync();
                    lock (sb)
                    {
                        sb.AppendLine(line);
                    }

                    if (printLog) Console.WriteLine("[Normal]" + head + line);
                }
            }, tk3.Token);

            var t2 = Task.Run(async () =>
            {
                using var sr = proc.StandardError;
                while (!sr.EndOfStream)
                {
                    tk3.Token.ThrowIfCancellationRequested();
                    var line = await sr.ReadLineAsync();
                    lock (sb)
                    {
                        sb.AppendLine(line);
                    }

                    if (printLog) Console.WriteLine("[Error]" + head + line);
                }
            }, tk3.Token);
            
            while (!proc.HasExited)
            {
                await Task.Yield();
            }

            tk2.Cancel();

            try
            {
                await Task.WhenAll(t1, t2);
            }
            catch (OperationCanceledException)
            {
                //ignore
            }

            if (printLog)
            {
                Console.WriteLine($"<color=red>{head}---------------Read end------------------</color>");
            }

            // Debug.Log($"Total execute time :{(proc.ExitTime-proc.StartTime).TotalMilliseconds} ms");//invalid in linux
            Console.WriteLine($"{head}ExitCode={proc.ExitCode}");
            return (proc.ExitCode == 0, sb.ToString(), proc.ExitCode);
        }

        public static (string FileName, string Argument) GetProcessCommand(string command)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ("CMD.exe", $"/c \"{command}\"");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return ("bash", $"-c \"sudo {command}\"");
            }

            throw new NotImplementedException("this platform haven't supported");
        }
    }
}