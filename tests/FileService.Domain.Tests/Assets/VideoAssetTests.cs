using FileService.Domain.Assets;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using FluentAssertions;

namespace FileService.Domain.Tests.Assets;

public class VideoAssetTests
{
    private const long ChunkSize = 5 * 1024 * 1024; // 5 MB для расчетов внутри MediaData

    private static StorageKey ValidStorageKey() =>
        StorageKey.Create("videos", "lessons", $"{Guid.NewGuid():N}").Value;

    private static MediaData ValidVideoData(long bytes = 1000) =>
        MediaData.Create(
            FileName.Create("movie.mp4").Value,
            ContentType.Create("video/mp4").Value,
            FileSize.Create(bytes).Value,
            expectedChunksCount: ChunkSize).Value; // Передаем размер чанка вместо количества

    private static MediaOwner ValidOwner() => MediaOwner.ForLesson(Guid.NewGuid()).Value;

    [Fact]
    public void Create_ValidData_Succeeds()
    {
        // Передаем обязательный StorageKey в фабричный метод
        var result = VideoAsset.Create(ValidVideoData(), ValidOwner(), ValidStorageKey());

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(MediaStatus.PENDING);
        result.Value.AssetType.Should().Be(AssetType.VIDEO);
        result.Value.StorageKey.Should().NotBeNull();
    }

    [Fact]
    public void Create_InvalidExtension_ReturnsError()
    {
        var data = MediaData.Create(
            FileName.Create("movie.exe").Value,
            ContentType.Create("video/mp4").Value,
            FileSize.Create(1000).Value,
            expectedChunksCount: ChunkSize).Value;

        var result = VideoAsset.Create(data, ValidOwner(), ValidStorageKey());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("video.invalid.extension");
    }

    [Fact]
    public void Create_WrongContentTypeCategory_ReturnsError()
    {
        var data = MediaData.Create(
            FileName.Create("movie.mp4").Value,
            ContentType.Create("image/png").Value,
            FileSize.Create(1000).Value,
            expectedChunksCount: ChunkSize).Value;

        var result = VideoAsset.Create(data, ValidOwner(), ValidStorageKey());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("video.invalid.content-type");
    }
}
