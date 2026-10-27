

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace photoHolidayContainer.data;

public sealed class VideoThumbnailService
{
    private readonly string _cachePath;

    private readonly ConcurrentDictionary<string, SemaphoreSlim>
        _locks = new();

    public VideoThumbnailService(string cachePath)
    {
        _cachePath = Path.GetFullPath(cachePath);
        Directory.CreateDirectory(_cachePath);
    }

    public async Task<string> GetThumbnailAsync(
        string videoPath,
        CancellationToken cancellationToken = default)
    {
        var info = new FileInfo(videoPath);

        if (!info.Exists)
            throw new FileNotFoundException(
                "Video not found.", videoPath);

        var key = string.Join("|",
            info.FullName,
            info.Length,
            info.LastWriteTimeUtc.Ticks);

        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(key)));

        var output = Path.Combine(_cachePath, hash + ".jpg");

        if (File.Exists(output))
            return output;

        var gate = _locks.GetOrAdd(
            hash, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(cancellationToken);

        try
        {
            if (File.Exists(output))
                return output;

            var temp = Path.Combine(
                _cachePath,
                Guid.NewGuid().ToString("N") + ".jpg");

            try
            {
                // Prefer a frame at two seconds.
                var success = await GenerateAsync(
                    videoPath, temp, "2", cancellationToken);

                // Very short clips may require an earlier frame.
                if (!success)
                {
                    if (File.Exists(temp))
                        File.Delete(temp);

                    success = await GenerateAsync(
                        videoPath, temp, "0", cancellationToken);
                }

                if (!success)
                    throw new InvalidOperationException(
                        "Unable to generate video thumbnail.");

                // Atomic publication within the same filesystem.
                File.Move(temp, output, overwrite: true);

                return output;
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task<bool> GenerateAsync(
        string input,
        string output,
        string seekSeconds,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);

        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        var start = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        foreach (var arg in new[]
        {
            "-nostdin",
            "-hide_banner",
            "-loglevel", "error",
            "-ss", seekSeconds,
            "-i", input,
            "-frames:v", "1",
            "-vf", "scale=640:-2",
            "-q:v", "3",
            "-update", "1",
            "-y", output
        })
        {
            start.ArgumentList.Add(arg);
        }

        using var process = new Process
        {
            StartInfo = start
        };

        process.Start();

        try
        {
            // Drain redirected output to avoid pipe deadlocks.
            var stderr = process.StandardError.ReadToEndAsync();
            var stdout = process.StandardOutput.ReadToEndAsync();

            await process.WaitForExitAsync(timeout.Token);
            await Task.WhenAll(stderr, stdout);

            return process.ExitCode == 0
                && File.Exists(output)
                && new FileInfo(output).Length > 0;
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);

            await process.WaitForExitAsync();

            cancellationToken.ThrowIfCancellationRequested();
            return false;
        }
    }
}

