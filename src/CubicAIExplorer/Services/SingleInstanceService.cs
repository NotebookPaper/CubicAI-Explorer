using System.IO;
using System.IO.Pipes;

namespace CubicAIExplorer.Services;

/// <summary>
/// Ensures only one instance of CubicAI Explorer runs at a time.
/// Uses a named mutex and named pipe for safe IPC (no WM_COPYDATA vulnerabilities).
/// </summary>
public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = "CubicAIExplorer_SingleInstance_Mutex";
    private const string PipeName = "CubicAIExplorer_SingleInstance_Pipe";

    private Mutex? _mutex;
    private CancellationTokenSource? _pipeCts;

    public event EventHandler<string[]>? ArgumentsReceived;

    public bool TryAcquire()
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (createdNew)
        {
            StartPipeServer();
            return true;
        }

        _mutex.Dispose();
        _mutex = null;
        return false;
    }

    public static void SendArgumentsToRunningInstance(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(3000);
            using var writer = new StreamWriter(client);
            writer.WriteLine(string.Join("|", args));
            writer.Flush();
        }
        catch
        {
            // Running instance may have closed; silently fail
        }
    }

    private void StartPipeServer()
    {
        _pipeCts = new CancellationTokenSource();
        var ct = _pipeCts.Token;

        Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                    await server.WaitForConnectionAsync(ct);
                    using var reader = new StreamReader(server);
                    var line = await reader.ReadLineAsync(ct);
                    if (!string.IsNullOrEmpty(line))
                    {
                        var args = line.Split('|', StringSplitOptions.RemoveEmptyEntries);
                        ArgumentsReceived?.Invoke(this, args);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { /* pipe error, retry */ }
            }
        }, ct);
    }

    public void Dispose()
    {
        _pipeCts?.Cancel();
        _pipeCts?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }
}
