using Core.Constants;
using FileService.Contracts;
using FileService.Domain;
using FileService.Domain.Assets;
using FileService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileService.Infrastructure.Postgres.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("media_assets");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.AssetType)
            .HasConversion<string>()
            .HasMaxLength(LengthConstants.SHORT_LENGTH)
            .HasColumnName("asset_type")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(LengthConstants.SHORT_LENGTH)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(x => x.IsTemporary)
            .HasColumnName("is_temporary")
            .IsRequired();

        builder.Property(x => x.UploadId)
            .HasColumnName("upload_id")
            .IsRequired(false);
        
        builder.Property(x => x.CreatedWhen)
            .HasColumnName("created_when")
            .IsRequired();

        builder.Property(x => x.UpdatedWhen)
            .HasColumnName("updated_when");

        // Метаданные
        builder.OwnsOne(x => x.MediaData, md =>
        {
            md.OwnsOne(m => m.FileName, fn =>
            {
                fn.Property(f => f.Name).HasColumnName("file_name").HasMaxLength(FileName.MAX_LENGTH).IsRequired();
                fn.Property(f => f.Extension).HasColumnName("file_extension").HasMaxLength(FileName.MAX_LENGTH).IsRequired();
                fn.Property(f => f.Value).HasColumnName("file_name_full").HasMaxLength(FileName.MAX_LENGTH * 2).IsRequired();
            });

            md.OwnsOne(m => m.ContentType, ct =>
            {
                ct.Property(c => c.Value).HasColumnName("content_type").HasMaxLength(ContentType.MAX_LENGTH).IsRequired();
                ct.Property(c => c.Category).HasColumnName("content_category").HasConversion<string>().HasMaxLength(ContentType.MAX_LENGTH).IsRequired();
            });

            md.OwnsOne(m => m.FileSize, fs =>
            {
                fs.Property(f => f.Bytes).HasColumnName("file_size_bytes").IsRequired();
            });

            md.Property(m => m.ExpectedChunksCount).HasColumnName("expected_chunks_count").IsRequired();
        });

        // Владелец файла
        builder.OwnsOne(x => x.MediaOwner, mo =>
        {
            mo.Property(o => o.Context).HasColumnName("owner_context").HasMaxLength(MediaOwner.MAX_LENGTH);
            mo.Property(o => o.EntityId).HasColumnName("owner_entity_id");
            mo.HasIndex(o => new { o.Context, o.EntityId }).HasDatabaseName("ix_media_assets_owner");
        });

        builder.OwnsOne(x => x.StorageKey, sk =>
        {
            sk.Property(k => k.Bucket)
                .HasColumnName("storage_bucket")
                .HasMaxLength(StorageKey.MAX_LENGTH);
            
            sk.Property(k => k.Prefix)
                .HasColumnName("storage_prefix")
                .HasMaxLength(StorageKey.MAX_LENGTH);
            
            sk.Property(k => k.Key)
                .HasColumnName("storage_key")
                .HasMaxLength(StorageKey.MAX_LENGTH);
            
            sk.Property(k => k.Value)
                .HasColumnName("storage_value")
                .HasMaxLength(StorageKey.MAX_LENGTH);

            sk.Property(k => k.FullPath)
                .HasColumnName("storage_full_path")
                .HasMaxLength(StorageKey.MAX_LENGTH);
            
            sk.HasIndex(k => new { k.Bucket, k.Prefix, k.Key })
                .IsUnique()
                .HasDatabaseName("ux_media_assets_storage_key");
        });
        
        builder.OwnsOne(a => a.StorageMetadata, sm =>
        {
            sm.Property(x => x.ContentType).HasColumnName("storage_metadata_actual_content_type");
            sm.Property(x => x.SizeBytes).HasColumnName("storage_metadata_actual_size_bytes");
            sm.Property(x => x.ETag).HasColumnName("storage_metadata_e_tag");
        });
        
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_media_assets_status");
        builder.HasIndex(x => x.AssetType).HasDatabaseName("ix_media_assets_asset_type");

        builder.HasDiscriminator<string>("asset_kind")
            .HasValue<VideoAsset>("video")
            .HasValue<DocumentAsset>("document")
            .HasValue<PreviewAsset>("preview")
            .HasValue<AudioAsset>("audio");
    }
}