using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnipLink.Domain.Entities;

namespace SnipLink.Infrastructure.Persistence.Configurations;

public class ShortLinkConfiguration : IEntityTypeConfiguration<ShortLink>
{
    public void Configure(EntityTypeBuilder<ShortLink> builder)
    {
        builder.ToTable("short_links");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(32);

        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.LongUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(x => x.OwnerToken)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasMany(x => x.Clicks)
            .WithOne(c => c.ShortLink!)
            .HasForeignKey(c => c.ShortLinkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.DailyStats)
            .WithOne(d => d.ShortLink!)
            .HasForeignKey(d => d.ShortLinkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
