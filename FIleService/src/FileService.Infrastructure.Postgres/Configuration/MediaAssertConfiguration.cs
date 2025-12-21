using FileService.Domain.Entities;
using FileService.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configuration;

public class MediaAssertConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("media_assets");

        builder.HasKey(m => m.Id);

        builder.HasDiscriminator(m => m.AssetType)
            .HasValue<VideoAsset>(AssetType.Video)
            .HasValue<PreviewAsset>(AssetType.Preview);
        
        builder.Property(m => m.AssetType)
            .HasConversion<string>()
            .HasColumnName("asset_type");

        builder.Property(m => m.Id).HasColumnName("id");

        builder.OwnsOne(m => m.MediaData, mb =>
        {
            mb.ToJson("media_data");

            mb.OwnsOne(md => md.FileName, fb =>
            {
                fb.Property(f => f.Name).HasColumnName("name");
                fb.Property(f => f.Extension).HasColumnName("extension");
            });

            mb.OwnsOne(md => md.ContentType, cb =>
            {
                cb.Property(c => c.Value).HasColumnName("value");
                cb.Property(c => c.Category).HasConversion<string>().HasColumnName("category");
            });

            mb.Property(md => md.Size).HasColumnName("size");
            mb.Property(md => md.ExpectedChunksCount).HasColumnName("expected_chunks_count");
        });

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasColumnName("status");

        builder.Property(m => m.CreatedAt).HasColumnName("created_at");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");

        builder.OwnsOne(m => m.RawKey, mb =>
        {
            mb.ToJson("raw_key");

            mb.Property(rk => rk.Key).HasColumnName("key");
            mb.Property(rk => rk.Prefix).HasColumnName("prefix").IsRequired(false);
            mb.Property(rk => rk.Bucket).HasColumnName("bucket");
            mb.Property(rk => rk.Value).HasColumnName("value");
            mb.Property(rk => rk.FullPath).HasColumnName("full_path");
        });

        builder.OwnsOne(m => m.FinalKey, mb =>
        {
            mb.ToJson("final_key");

            mb.Property(rk => rk.Key).HasColumnName("key");
            mb.Property(rk => rk.Prefix).HasColumnName("prefix").IsRequired(false);
            mb.Property(rk => rk.Bucket).HasColumnName("bucket");
            mb.Property(rk => rk.Value).HasColumnName("value");
            mb.Property(rk => rk.FullPath).HasColumnName("full_path");
        });

        builder.OwnsOne(m => m.Owner, mb =>
        {
            mb.ToJson("owner");

            mb.Property(o => o.Context).HasColumnName("context");
            mb.Property(o => o.EntityId).HasColumnName("context_id");
        });

        builder.HasIndex(m => new
        {
            m.Status, m.CreatedAt
        });
    }
}