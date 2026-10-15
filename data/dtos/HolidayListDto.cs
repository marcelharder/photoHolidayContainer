namespace photoHolidayContainer.data.dtos;
public record HolidayListDto(
    int Id,
    string Title,
    string? Description,
    string? CoverImageUrl,
    DateTime? StartDate,
    DateTime? EndDate
);
