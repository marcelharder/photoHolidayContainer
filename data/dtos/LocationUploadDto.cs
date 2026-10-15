using photoHolidayContainer.data.dtos;

public class LocationUploadDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? ImageDirectory { get; set; }

    public List<LocationImageUploadDto> Images { get; set; } = new();
}