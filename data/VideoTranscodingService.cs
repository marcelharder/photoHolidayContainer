
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace photoHolidayContainer.data;

public sealed class VideoTranscodingService
{
    private readonly string _cachePath;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public VideoTranscodingService(string cachePath)
    {
        _cachePath = Path.GetFullPath(cachePath);
        Directory.CreateDirectory(_cachePath);
    }

    public async Task<string> GetBrowserVideoAsync(
        string videoPath,
        CancellationToken cancellationToken = default)
    {
        var info = new FileInfo(videoPath);

        if (!info.Exists)
            throw new FileNotFoundException("Video not found.", videoPath);

        var key = string.Join("|",
            info.FullName,
            info.Length,
            info.LastWriteTimeUtc.Ticks,
            "h264-1080p-v1");

        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(key)));

        var output = Path.Combine(_cachePath, hash[..8] + ".mp4");

        if (File.Exists(output) && new FileInfo(output).Length > 0)
            return output;

        var gate = _locks.GetOrAdd(
            hash, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(cancellationToken);

        try
        {
            if (File.Exists(output) && new FileInfo(output).Length > 0)
                return output;

            var temp = Path.Combine(
                _cachePath,
                Guid.NewGuid().ToString("N") + ".mp4");

            try
            {
                await ConvertAsync(
                    videoPath,
                    temp,
                    cancellationToken);

                if (!File.Exists(temp) || new FileInfo(temp).Length == 0)
                    throw new InvalidOperationException(
                        "Video conversion produced no output.");

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

    private static async Task ConvertAsync(
        string input,
        string output,
        CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = false
        };

        foreach (var arg in new[]
        {
            "-nostdin",
            "-hide_banner",
            "-loglevel", "error",
            "-i", input,
            "-map", "0:v:0",
            "-map", "0:a?",
            "-c:v", "libx264",
            "-preset", "medium",
            "-crf", "23",
            "-vf", "scale=w='min(1920,iw)':h=-2",
            "-pix_fmt", "yuv420p",
            "-c:a", "aac",
            "-b:a", "128k",
            "-movflags", "+faststart",
            "-y", output
        })
        {
            start.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = start };

        process.Start();

        // Read errors asynchronously so FFmpeg cannot block
        // on a full stderr pipe.
        var stderrTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken);

            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"FFmpeg video conversion failed: {stderr}");
            }
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            throw;
        }
    }
}
