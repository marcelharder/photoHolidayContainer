namespace photoHolidayContainer.data.dtos;
public record LocationImageDto(
    int Id,
    string ImageUrl,
    string? Caption
)
{
    public bool IsVideo => 
      Path.GetExtension(ImageUrl).ToLowerInvariant() is "mp4" or "mov"
}