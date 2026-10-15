

namespace photoHolidayContainer.data.models;

public class Holiday
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string? CoverImageUrl { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Navigation
    public List<Location> Locations { get; set; } = new();
}

