using System.Diagnostics;
using System.Runtime.CompilerServices;
using SharedLibrary.Extensions;
using Xabe.FFmpeg;

namespace SharedLibrary;

public class FFmpegOnPipes : FFmpeg
{
    public async IAsyncEnumerable<byte[]> ConvertStream(IAsyncEnumerable<byte[]> inputData, string args, [EnumeratorCancellation] CancellationToken token)
    {
        ProcessPriorityClass? priority = null;

        const int ChunkSize = 4096 + 2 * 2 * 24000;

        using var process = RunProcess(
                args: args,
                processPath: FFmpegPath,
                priority: priority,
                standardInput: true,
                standardOutput: true,
                standardError: false);
        try
        {
            var startTime = DateTime.Now;
            if (process is not null)
            {
                var reatTask = Task.Run(async () =>
                {
                    await foreach (var item in inputData)
                    {
                        await process.StandardInput.BaseStream.WriteAsync(item, token);
                    }
                    process.StandardInput.Close();
                });

                using var result = process.StandardOutput.BaseStream;
                byte[] buffer = new byte[ChunkSize];
                int readCount = 0;
                byte[] bytesRead = Array.Empty<byte>();
                while ((readCount = await result.ReadAsync(buffer, token)) > 0)
                {
                    bytesRead = bytesRead.Concat(buffer.Take(readCount)).ToArray();
                    if (bytesRead.Length > 8000)
                    {
                        var b = bytesRead;
                        bytesRead = Array.Empty<byte>();
                        yield return b;
                    }
                }

                if (bytesRead.Length > 0)
                {
                    yield return bytesRead;
                }

                result.Close();
                await result.DisposeAsync();
                process.StandardOutput.Close();

                await reatTask;
                await process.WaitForExitAsync();
                var exitCode = process.ExitCode;
                Console.WriteLine($"exitCode: {exitCode}");

            }
            var finishTime = DateTime.Now;
            var processingTime = finishTime - startTime;
            Console.WriteLine($"processingTime (c): {processingTime.TotalSeconds}");
        }
        finally
        {
            if (process is not null)
            {
                await process.DisposeAsync();
            }
        }
        yield break;
    }

    public async Task<byte[]> ConvertPartSound(ReadOnlyMemory<byte> inputData, string args, CancellationToken token)
    {
        ProcessPriorityClass? priority = null;

        const int ChunkSize = 4096 + 2 * 2 * 24000;

        using var process = RunProcess(
                args: args,
                processPath: FFmpegPath,
                priority: priority,
                standardInput: true,
                standardOutput: true,
                standardError: false);

        List<byte> responseArray = new();
        try
        {
            var startTime = DateTime.Now;
            if (process is not null)
            {
                var reatTask = Task.Run(async () =>
                {
                    await process.StandardInput.BaseStream.WriteAsync(inputData, token);
                    process.StandardInput.Close();
                });

                using var result = process.StandardOutput.BaseStream;
                byte[] buffer = new byte[ChunkSize];
                int readCount = 0;
                while ((readCount = await result.ReadAsync(buffer, token)) > 0)
                {
                    responseArray.AddRange(buffer.Take(readCount));
                }

                result.Close();
                await result.DisposeAsync();
                process.StandardOutput.Close();

                await reatTask;
                await process.WaitForExitAsync();

                var exitCode = process.ExitCode;
                Console.WriteLine($"exitCode: {exitCode}");
                //Console.WriteLine($"/////////////////////////////////////////////Input length {inputData.Length}, output length {responseArray.Count}");
            }
            var finishTime = DateTime.Now;
            var processingTime = finishTime - startTime;
            Console.WriteLine($"processingTime (c): {processingTime.TotalSeconds}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            if (process is not null)
            {
                await process.DisposeAsync();
            }
        }
        return responseArray.ToArray();
    }

}
