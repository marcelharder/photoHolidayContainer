namespace photoHolidayContainer.data.dtos;
public record HolidayWithLocationsDto(
    int Id,
    string Title,
    string? Description,
    string? CoverImageUrl,
    DateTime? StartDate,
    DateTime? EndDate,
    List<LocationDto> Locations
);