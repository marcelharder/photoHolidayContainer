using System.Text.Json.Serialization;

namespace photoHolidayContainer.data.models;
public class Location
{
    public int Id { get; set; }

    public int HolidayId { get; set; }

    [JsonIgnore]
    public required Holiday Holiday { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Navigation
    public List<LocationImage> Images { get; set; } = new();
}
