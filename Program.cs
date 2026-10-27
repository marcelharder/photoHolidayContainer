
using Microsoft.EntityFrameworkCore;
using photoHolidayContainer.data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

var imageRootPath = builder.Configuration["ImageStorage:RootPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "images");

builder.Services.AddSingleton(
    new LocationImageStorage(imageRootPath));

// New: thumbnail cache
var thumbnailCachePath =
    builder.Configuration["VideoThumbnails:CachePath"]
    ?? Path.Combine(
        builder.Environment.ContentRootPath,
        "thumbnail-cache");

builder.Services.AddSingleton<VideoConversionQueue>();

builder.Services.AddHostedService<VideoConversionWorker>();

builder.Services.AddSingleton(
    new VideoTranscodingService(
        builder.Configuration["VideoTranscoding:CachePath"]
        ?? "/app/video-cache"));

builder.Services.AddSingleton(
    new VideoThumbnailService(thumbnailCachePath));

builder.Services.AddDbContext<TravelContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(12, 2))
    )
);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<TravelContext>();

    db.Database.Migrate();
    DbInitializer.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
