using Microsoft.EntityFrameworkCore;

namespace BusinessService.Data;

public class BusinessDbContext : DbContext
{
    public BusinessDbContext(DbContextOptions<BusinessDbContext> options) : base(options) { }

    public DbSet<Business> Businesses => Set<Business>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Business>(entity =>
        {
            entity.HasKey(e => e.BusinessId);
            entity.Property(e => e.BusinessId).HasMaxLength(36);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(64);
            entity.Property(e => e.Geohash).HasMaxLength(12).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(512);
            entity.Property(e => e.Phone).HasMaxLength(32);

            entity.HasIndex(e => e.Geohash).HasDatabaseName("IX_Business_Geohash");
            entity.HasIndex(e => new { e.Geohash, e.Category }).HasDatabaseName("IX_BusinessLocation_Geohash_Category");
        });
    }
}

public class Business
{
    public string BusinessId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Geohash { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public double Rating { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
