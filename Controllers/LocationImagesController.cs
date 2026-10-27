using Microsoft.AspNetCore.Mvc;
using photoHolidayContainer.data;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace photoHolidayContainer.Controllers;

[ApiController]
[Route("api/location-images")]
public sealed class LocationImagesController(
    VideoThumbnailService thumbnailService,
    LocationImageStorage imageStorage,
    TravelContext db) : ControllerBase
{
    private const string ImageUrlPrefix = "/api/location-images/";
    private const int MaxImageDimension = 1920;

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetImageById(int id)
    {
        var image = await db.LocationImages
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (image is null
            || !image.ImageUrl.StartsWith(ImageUrlPrefix, StringComparison.Ordinal)
            || !TryGetStoredImage(image.ImageUrl, out var fullPath))
        {
            return NotFound();
        }

        return await CreateImageResult(fullPath);
    }

    [HttpGet("{**imagePath}")]
    public async Task<IActionResult> GetImage(string imagePath)
    {
        if (!imageStorage.TryGetImage(imagePath, out var fullPath))
        {
            return NotFound();
        }

        return await CreateImageResult(fullPath);
    }




[HttpGet("{id:int}/thumbnail")]
public async Task<IActionResult> GetVideoThumbnail(
    int id,
    CancellationToken cancellationToken)
{
    var image = await db.LocationImages
        .AsNoTracking()
        .FirstOrDefaultAsync(
            x => x.Id == id,
            cancellationToken);

    if (image is null)
    {
        return NotFound();
    }

    // TryGetStoredImage already returns the full physical path.
    if (!image.ImageUrl.StartsWith(
            ImageUrlPrefix,
            StringComparison.Ordinal)
        || !TryGetStoredImage(
            image.ImageUrl,
            out var fullPath))
    {
        return NotFound();
    }

    // Only videos can have video thumbnails.
    if (!LocationImageStorage.IsVideo(fullPath))
    {
        return NotFound();
    }

    // Generate the thumbnail or retrieve it from cache.
    var thumbnailPath = await thumbnailService
        .GetThumbnailAsync(
            fullPath,
            cancellationToken);

    return PhysicalFile(
        thumbnailPath,
        "image/jpeg");
}




    private bool TryGetStoredImage(string imageUrl, out string fullPath)
    {
        var encodedPath = imageUrl[ImageUrlPrefix.Length..];
        try
        {
            var relativePath = string.Join(
                Path.DirectorySeparatorChar,
                encodedPath.Split('/')
                    .Select(Uri.UnescapeDataString));
            return imageStorage.TryGetImage(relativePath, out fullPath);
        }
        catch (UriFormatException)
        {
            fullPath = string.Empty;
            return false;
        }
    }

    /* private async Task<IActionResult> CreateImageResult(string fullPath)
    {
        if (Path.GetExtension(fullPath).Equals(".avif", StringComparison.OrdinalIgnoreCase))
        {
            return PhysicalFile(fullPath, "image/avif", enableRangeProcessing: true);
        }

        using var image = await Image.LoadAsync(fullPath, HttpContext.RequestAborted);
        var format = image.Metadata.DecodedImageFormat
            ?? throw new InvalidDataException($"Unable to determine image format for '{fullPath}'.");

        if (image.Width > MaxImageDimension || image.Height > MaxImageDimension)
        {
            image.Mutate(operation => operation.Resize(new ResizeOptions
            {
                Size = new Size(MaxImageDimension, MaxImageDimension),
                Mode = ResizeMode.Max
            }));
        }

        var resizedImage = new MemoryStream();
        await image.SaveAsync(resizedImage, format, HttpContext.RequestAborted);
        resizedImage.Position = 0;

        return File(resizedImage, format.DefaultMimeType, enableRangeProcessing: true);
    } */

    private Task<IActionResult> CreateImageResult(string fullPath)
{
    if (Path.GetExtension(fullPath).Equals(
        ".avif", StringComparison.OrdinalIgnoreCase))
    {
        return Task.FromResult<IActionResult>(
            PhysicalFile(
                fullPath,
                "image/avif",
                enableRangeProcessing: true));
    }

if (LocationImageStorage.IsVideo(fullPath))
{
    var contentType = Path.GetExtension(fullPath)
        .ToLowerInvariant() switch
    {
        ".mp4" => "video/mp4",
        ".mov" => "video/quicktime",
        _ => "application/octet-stream"
    };

    return Task.FromResult<IActionResult>(
        PhysicalFile(
            fullPath,
            contentType,
            enableRangeProcessing: true));
}
    using var bitmap = SKBitmap.Decode(fullPath);

    if (bitmap == null)
    {
        throw new InvalidDataException(
            $"Unable to decode image '{fullPath}'.");
    }

    // No resizing required
    if (bitmap.Width <= MaxImageDimension &&
        bitmap.Height <= MaxImageDimension)
    {
        return Task.FromResult<IActionResult>(
            PhysicalFile(
                fullPath,
                GetImageMimeType(fullPath),
                enableRangeProcessing: true));
    }

    // Preserve aspect ratio
    double scale = Math.Min(
        (double)MaxImageDimension / bitmap.Width,
        (double)MaxImageDimension / bitmap.Height);

    int newWidth = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
    int newHeight = Math.Max(1, (int)Math.Round(bitmap.Height * scale));

    var imageInfo = new SKImageInfo(newWidth, newHeight);

    using var resized = bitmap.Resize(
        imageInfo,
        new SKSamplingOptions(
            SKFilterMode.Linear,
            SKMipmapMode.Linear));

    if (resized == null)
    {
        throw new InvalidOperationException("Image resize failed.");
    }

    using var image = SKImage.FromBitmap(resized);
    using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);

    if (data == null)
    {
        throw new InvalidOperationException("Image encoding failed.");
    }

    var stream = new MemoryStream(data.ToArray());

    return Task.FromResult<IActionResult>(
        File(stream, "image/jpeg"));
}

private static string GetImageMimeType(string path)
{
    return Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".avif" => "image/avif",
        _ => "application/octet-stream"
    };
}
}
