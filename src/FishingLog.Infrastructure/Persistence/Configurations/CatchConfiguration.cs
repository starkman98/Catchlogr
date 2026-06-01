using FishingLog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FishingLog.Infrastructure.Persistence.Configurations;

public class CatchConfiguration : IEntityTypeConfiguration<Catch>
{
    public void Configure(EntityTypeBuilder<Catch> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasOne<FishingTrip>()
            .WithMany(t => t.Catches)
            .HasForeignKey(c => c.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.Species)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Note)
            .HasMaxLength(2000);

        builder.Property(c => c.CaughtAt)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.Property(c => c.LastModified)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        builder.HasIndex(c => c.LastModified);

        builder.OwnsOne(c => c.Bait, bait =>
        {
            bait.Property(b => b.Name).HasMaxLength(100);
            bait.Property(b => b.Color).HasMaxLength(50);
        });
    }
}