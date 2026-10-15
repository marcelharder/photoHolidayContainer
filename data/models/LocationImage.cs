using System.Text.Json.Serialization;

namespace photoHolidayContainer.data.models;
public class LocationImage
{
    public int Id { get; set; }

    public int LocationId { get; set; }
    
    [JsonIgnore]
    public required Location Location { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
}
