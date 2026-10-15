namespace photoHolidayContainer.data.dtos;
public class HolidayUploadDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public List<LocationUploadDto> Locations { get; set; } = new();
}