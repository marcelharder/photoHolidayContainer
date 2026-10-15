namespace photoHolidayContainer.data.dtos;
public record LocationDto(
    int Id,
    string Name,
    string? Description,
    double? Latitude,
    double? Longitude,
    List<LocationImageDto> Images
);