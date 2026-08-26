using FishingLog.Domain.Entities;
using FishingLog.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FishingLog.Infrastructure.Persistence;

/// <summary>
/// Represents the PostgreSQL database context for FishingLog.
/// </summary>
public sealed class FishingLogDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    /// <summary>
    /// Gets the fishing-trip data set.
    /// </summary>
    public DbSet<FishingTrip> FishingTrips => Set<FishingTrip>();

    /// <summary>
    /// Gets the catch data set.
    /// </summary>
    public DbSet<Catch> Catches => Set<Catch>();

    /// <summary>Gets the private catch-photo metadata set.</summary>
    public DbSet<CatchPhoto> CatchPhotos => Set<CatchPhoto>();

    /// <summary>
    /// Initializes the database context.
    /// </summary>
    public FishingLogDbContext(
        DbContextOptions<FishingLogDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FishingLogDbContext).Assembly);
    }
}
