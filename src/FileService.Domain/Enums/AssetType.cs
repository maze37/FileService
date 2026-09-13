namespace FileService.Domain.Enums;

/// <summary>
/// Назначение файла — определяет конкретный подтип MediaAsset и bucket в S3.
/// </summary>
public enum AssetType
{
    VIDEO,
    AUDIO,
    DOCUMENT,
    AVATAR,
    COVER,
    PREVIEW,
    THUMBNAIL
}