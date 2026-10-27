
namespace photoHolidayContainer.data;

public sealed class VideoConversionWorker(
    VideoConversionQueue queue,
    VideoTranscodingService transcodingService,
    ILogger<VideoConversionWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await foreach (var videoPath in
            queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                logger.LogInformation(
                    "Converting holiday video: {VideoPath}",
                    videoPath);

                var output = await transcodingService
                    .GetBrowserVideoAsync(
                        videoPath,
                        stoppingToken);

                logger.LogInformation(
                    "Holiday video ready: {Output}",
                    output);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to convert holiday video: {VideoPath}",
                    videoPath);
            }
        }
    }
}
