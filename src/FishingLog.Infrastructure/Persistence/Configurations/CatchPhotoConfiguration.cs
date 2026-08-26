using FishingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FishingLog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures private catch-photo metadata for PostgreSQL.
/// </summary>
public sealed class CatchPhotoConfiguration : IEntityTypeConfiguration<CatchPhoto>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<CatchPhoto> builder)
    {
        builder.HasKey(photo => photo.Id);
        builder.Property(photo => photo.StorageKey).IsRequired().HasMaxLength(500);
        builder.Property(photo => photo.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(photo => photo.CreatedAtUtc)
            .HasConversion(
                value => value,
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        builder.HasOne(photo => photo.Catch)
            .WithOne(currentCatch => currentCatch.Photo)
            .HasForeignKey<CatchPhoto>(photo => photo.CatchId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(photo => photo.CatchId).IsUnique();
        builder.HasIndex(photo => photo.StorageKey).IsUnique();
    }
}
