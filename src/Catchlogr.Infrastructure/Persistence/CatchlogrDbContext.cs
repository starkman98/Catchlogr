using Catchlogr.Domain.Entities;
using Catchlogr.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Catchlogr.Infrastructure.Persistence;

/// <summary>
/// Represents the PostgreSQL database context for Catchlogr.
/// </summary>
public sealed class CatchlogrDbContext
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
    public CatchlogrDbContext(
        DbContextOptions<CatchlogrDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CatchlogrDbContext).Assembly);
    }
}
