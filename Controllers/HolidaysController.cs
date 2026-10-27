using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using photoHolidayContainer.data;
using photoHolidayContainer.data.dtos;
using photoHolidayContainer.data.models;

namespace photoHolidayContainer.Controllers;

[ApiController]
[Route("api/holidays")]
public sealed class HolidaysController(
    TravelContext db,
    VideoConversionQueue videoConversionQueue,
    LocationImageStorage imageStorage) : ControllerBase
{


    [HttpGet]
    public async Task<IActionResult> GetHolidays([FromQuery] int page, [FromQuery] int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = db.Holidays
            .OrderByDescending(holiday => holiday.StartDate);

        var totalCount = await query.CountAsync();
        var holidays = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(holiday => new HolidayListDto(
                holiday.Id,
                holiday.Title,
                holiday.Description,
                holiday.CoverImageUrl,
                holiday.StartDate,
                holiday.EndDate
            ))
            .ToListAsync();

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = holidays
        });
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPagedHolidays([FromQuery] int page, [FromQuery] int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = db.Holidays
            .Include(holiday => holiday.Locations)
                .ThenInclude(location => location.Images)
            .OrderByDescending(holiday => holiday.StartDate);

        var totalCount = await query.CountAsync();
        var holidays = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(holiday => new HolidayWithLocationsDto(
                holiday.Id,
                holiday.Title,
                holiday.Description,
                holiday.CoverImageUrl,
                holiday.StartDate,
                holiday.EndDate,
                holiday.Locations.Select(location => new LocationDto(
                    location.Id,
                    location.Name,
                    location.Description,
                    location.Latitude,
                    location.Longitude,
                    location.Images.Select(image => new LocationImageDto(
                        image.Id,
                        image.ImageUrl,
                        image.Caption
                    )).ToList()
                )).ToList()
            ))
            .ToListAsync();

        return Ok(new
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = holidays
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetHoliday(int id)
    {
        var holiday = await db.Holidays
            .Include(item => item.Locations)
                .ThenInclude(location => location.Images)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (holiday is null)
        {
            return NotFound();
        }

        return Ok(ToHolidayWithLocationsDto(holiday));
    }

    
[HttpPost("upload")]
public async Task<IActionResult> UploadHoliday(HolidayUploadDto dto)
{
    // Collect video paths from all locations.
    var videoPaths = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase);

    var holiday = new Holiday
    {
        Title = dto.Title,
        Description = dto.Description,
        CoverImageUrl = dto.CoverImageUrl,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate
    };

    foreach (var locationDto in dto.Locations)
    {
        var location = new Location
        {
            Holiday = holiday,
            Name = locationDto.Name,
            Description = locationDto.Description,
            Latitude = locationDto.Latitude,
            Longitude = locationDto.Longitude
        };

        foreach (var imageDto in locationDto.Images)
        {
            location.Images.Add(new LocationImage
            {
                Location = location,
                ImageUrl = imageDto.ImageUrl ?? string.Empty,
                Caption = imageDto.Caption
            });
        }

        if (!string.IsNullOrWhiteSpace(locationDto.ImageDirectory))
        {
            if (!imageStorage.TryGetImages(
                locationDto.ImageDirectory,
                out var imagePaths))
            {
                Console.WriteLine(
                    $"ImageDirectory: {locationDto.ImageDirectory}");

                return BadRequest(new
                {
                    Error = "ImageDirectory must refer to an existing directory under the configured image root."
                });
            }

            foreach (var imagePath in imagePaths)
            {
                location.Images.Add(new LocationImage
                {
                    Location = location,
                    ImageUrl = imageStorage.GetImageUrl(imagePath)
                });

                // imagePath is already the full physical path
                // returned by TryGetImages().
                if (LocationImageStorage.IsVideo(imagePath))
                {
                    videoPaths.Add(imagePath);
                }
            }
        }

        holiday.Locations.Add(location);
    }

    // Save the holiday and its locations/images first.
    db.Holidays.Add(holiday);
    await db.SaveChangesAsync();

    // Queue conversion only after successful database save.
    foreach (var videoPath in videoPaths)
    {
        await videoConversionQueue.QueueAsync(videoPath);
    }

    return Created(
        $"/api/holidays/{holiday.Id}",
        ToHolidayWithLocationsDto(holiday));
}


    [HttpDelete("deleteHoliday/{id}")]
    public async Task<IActionResult> DeleteHoliday(int id)
    {
        var holiday = await db.Holidays
            .FirstOrDefaultAsync(h => h.Id == id);

        if (holiday == null)
        {
            return NotFound(new
            {
                message = $"Holiday with ID {id} not found."
            });
        }

        db.Holidays.Remove(holiday);
        await db.SaveChangesAsync();

        return Ok(new
        {
            message = $"Holiday with ID {id} deleted successfully."
        });
    }


    private static HolidayWithLocationsDto ToHolidayWithLocationsDto(Holiday holiday) =>
        new(
            holiday.Id,
            holiday.Title,
            holiday.Description,
            holiday.CoverImageUrl,
            holiday.StartDate,
            holiday.EndDate,
            holiday.Locations.Select(ToLocationDto).ToList()
        );

    private static LocationDto ToLocationDto(Location location) =>
        new(
            location.Id,
            location.Name,
            location.Description,
            location.Latitude,
            location.Longitude,
            location.Images.Select(image => new LocationImageDto(
                image.Id,
                image.ImageUrl,
                image.Caption
            )).ToList()
        );
}
