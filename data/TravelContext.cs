using Microsoft.EntityFrameworkCore;
using photoHolidayContainer.data.models;

namespace photoHolidayContainer.data;
public class TravelContext : DbContext
{
    public DbSet<Holiday> Holidays { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<LocationImage> LocationImages { get; set; }

    public TravelContext(DbContextOptions<TravelContext> options)
        : base(options)
    {
    }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Relationships
    modelBuilder.Entity<Holiday>()
        .HasMany(h => h.Locations)
        .WithOne(l => l.Holiday)
        .HasForeignKey(l => l.HolidayId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<Location>()
        .HasMany(l => l.Images)
        .WithOne(i => i.Location)
        .HasForeignKey(i => i.LocationId)
        .OnDelete(DeleteBehavior.Cascade);

    
    
}

}
