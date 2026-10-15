using photoHolidayContainer.data.models;

namespace photoHolidayContainer.data;
public static class DbInitializer
{
    public static void Seed(TravelContext context)
    {
        if (context.Holidays.Any())
            return; // Already seeded

        var holiday = new Holiday
        {
            Id = 1,
            Title = "Summer Trip to Italy",
            Description = "A beautiful holiday exploring Rome and Venice.",
            CoverImageUrl = "https://example.com/holidays/italy-cover.jpg",
            StartDate = new DateTime(2024, 7, 10),
            EndDate = new DateTime(2024, 7, 20)
        };

        var locations = new List<Location>
        {
            new Location
            {
                Id = 1,
                HolidayId = 1,
                Holiday = holiday,
                Name = "Rome – Colosseum",
                Description = "Visited the ancient Colosseum.",
                Latitude = 41.8902,
                Longitude = 12.4922
            },
            new Location
            {
                Id = 2,
                HolidayId = 1,
                Holiday = holiday,
                Name = "Venice – Rialto Bridge",
                Description = "Walked across the famous Rialto Bridge.",
                Latitude = 45.4380,
                Longitude = 12.3358
            }
        };

        var images = new List<LocationImage>
        {
            new LocationImage { Id = 1, LocationId = 1, Location = locations[0], ImageUrl = "https://example.com/locations/colosseum1.jpg", Caption = "Sunny day at the Colosseum" },
            new LocationImage { Id = 2, LocationId = 1, Location = locations[0], ImageUrl = "https://example.com/locations/colosseum2.jpg", Caption = "Inside the arena" },
            new LocationImage { Id = 3, LocationId = 2, Location = locations[1], ImageUrl = "https://example.com/locations/rialto1.jpg", Caption = "Rialto Bridge view" },
            new LocationImage { Id = 4, LocationId = 2, Location = locations[1], ImageUrl = "https://example.com/locations/rialto2.jpg", Caption = "Evening lights" }
        };

        context.Holidays.Add(holiday);
        context.Locations.AddRange(locations);
        context.LocationImages.AddRange(images);

        context.SaveChanges();
    }
}
