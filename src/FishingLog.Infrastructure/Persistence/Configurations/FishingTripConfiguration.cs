
using FishingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FishingLog.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures database persistence for fishing trips.
/// </summary>
public sealed class FishingTripConfiguration
    : IEntityTypeConfiguration<FishingTrip>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FishingTrip> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.LocationName)
            .HasMaxLength(100);

        builder.Property(t => t.WeatherDescription)
            .HasMaxLength(500);

        builder.Property(t => t.Note)
            .HasMaxLength(2000);

        builder.Property(t => t.WeatherProvider)
            .HasMaxLength(100);

        builder.Property(t => t.MoonPhase)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(t => t.StartTime)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Property(t => t.EndTime)
            .HasConversion(v => v, v => v.HasValue
            ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
            : v);

        builder.Property(t => t.CreatedAt)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Property(t => t.LastModified)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        
        builder.HasIndex(t => new { t.LastModified, t.UserId });

        builder.Property(t => t.WeatherSampleTimeUtc)
            .HasConversion(v => v, v => v.HasValue 
            ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
            : v);
    }
}
