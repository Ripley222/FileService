using FileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configuration;

public class VideoAssertConfiguration : IEntityTypeConfiguration<VideoAsset>
{
    public void Configure(EntityTypeBuilder<VideoAsset> builder)
    {
        builder.OwnsOne(v => v.HlsRootKey, vb =>
        {
            vb.ToJson("hls_root_key");
            
            vb.Property(rk => rk.Key).HasColumnName("key");
            vb.Property(rk => rk.Prefix).HasColumnName("prefix").IsRequired(false);
            vb.Property(rk => rk.Bucket).HasColumnName("bucket");
            vb.Property(rk => rk.Value).HasColumnName("value");
            vb.Property(rk => rk.FullPath).HasColumnName("full_path");
        });
    }
}