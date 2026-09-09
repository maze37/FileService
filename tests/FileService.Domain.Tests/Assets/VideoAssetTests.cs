using FileService.Domain.Assets;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using FluentAssertions;

namespace FileService.Domain.Tests.Assets;

public class VideoAssetTests
{
    private static MediaData ValidVideoData(long bytes = 1000) =>
        MediaData.Create(
            FileName.Create("movie.mp4").Value,
            ContentType.Create("video/mp4").Value,
            FileSize.Create(bytes).Value,
            expectedChunksCount: 1).Value;

    private static MediaOwner ValidOwner() => MediaOwner.ForLesson(Guid.NewGuid()).Value;

    [Fact]
    public void Create_ValidData_Succeeds()
    {
        var result = VideoAsset.Create(ValidVideoData(), ValidOwner());

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(MediaStatus.PENDING);
        result.Value.AssetType.Should().Be(FileService.Domain.Enums.AssetType.VIDEO);
    }

    [Fact]
    public void Create_InvalidExtension_ReturnsError()
    {
        var data = MediaData.Create(
            FileName.Create("movie.exe").Value,
            ContentType.Create("video/mp4").Value,
            FileSize.Create(1000).Value,
            1).Value;

        var result = VideoAsset.Create(data, ValidOwner());

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
            1).Value;

        var result = VideoAsset.Create(data, ValidOwner());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("video.invalid.content-type");
    }
}