using FishingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FishingLog.Infrastructure.Persistence;

public class FishingLogDbContext : DbContext
{
    public DbSet<FishingTrip> FishingTrips => Set<FishingTrip>();
    public DbSet<Catch> Catches => Set<Catch>();

    public FishingLogDbContext(DbContextOptions<FishingLogDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FishingLogDbContext).Assembly);
    }

}
