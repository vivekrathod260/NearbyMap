using Microsoft.EntityFrameworkCore;

namespace ProximityService.Data;

public class ProximityDbContext : DbContext
{
    public ProximityDbContext(DbContextOptions<ProximityDbContext> options) : base(options) { }

    public DbSet<BusinessLocation> BusinessLocations => Set<BusinessLocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BusinessLocation>(entity =>
        {
            // Read from the shared Businesses table owned by BusinessService
            entity.ToTable("Businesses");

            entity.HasKey(e => e.BusinessId);
            entity.Property(e => e.BusinessId).HasMaxLength(36);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(64);
            entity.Property(e => e.Geohash).HasMaxLength(12).IsRequired();

            // Critical indexes for read-heavy geospatial queries
            entity.HasIndex(e => e.Geohash).HasDatabaseName("IX_Business_Geohash");
            entity.HasIndex(e => new { e.Geohash, e.Category }).HasDatabaseName("IX_BusinessLocation_Geohash_Category");
        });
    }
}

public class BusinessLocation
{
    public string BusinessId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Geohash { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Rating { get; set; }
}
