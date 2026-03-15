using System.IO;
using System.IO.Pipes;
using System.Text;

namespace CubicAIExplorer.Services;

/// <summary>
/// Ensures only one instance of CubicAI Explorer runs at a time.
/// Uses a named mutex and named pipe for safe IPC (no WM_COPYDATA vulnerabilities).
/// </summary>
public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = "CubicAIExplorer_SingleInstance_Mutex";
    private const string PipeName = "CubicAIExplorer_SingleInstance_Pipe";
    private const int MaxPipeMessageBytes = 4096;

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
                    var line = await ReadPipeMessageAsync(server, ct);
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

    private static async Task<string?> ReadPipeMessageAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[256];
        using var payload = new MemoryStream();

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (bytesRead == 0)
                break;

            var newlineIndex = Array.IndexOf(buffer, (byte)'\n', 0, bytesRead);
            var bytesToWrite = newlineIndex >= 0 ? newlineIndex : bytesRead;
            payload.Write(buffer, 0, bytesToWrite);

            if (payload.Length > MaxPipeMessageBytes)
                return null;

            if (newlineIndex >= 0)
                break;
        }

        if (payload.Length == 0)
            return null;

        return Encoding.UTF8.GetString(payload.ToArray()).TrimStart('\uFEFF').TrimEnd('\r');
    }

    public void Dispose()
    {
        _pipeCts?.Cancel();
        _pipeCts?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }
}
