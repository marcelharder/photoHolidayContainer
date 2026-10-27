namespace photoHolidayContainer.data.dtos;
public record LocationImageDto(
    int Id,
    string ImageUrl,
    string? Caption)
{
    public bool IsVideo =>
        Uri.UnescapeDataString(ImageUrl)
            .Split('?', '#')[0]
            .EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)
        || Uri.UnescapeDataString(ImageUrl)
            .Split('?', '#')[0]
            .EndsWith(".mov", StringComparison.OrdinalIgnoreCase);
}