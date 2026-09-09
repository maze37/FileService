using FileService.Domain.Assets;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using FluentAssertions;

namespace FileService.Domain.Tests;

public class MediaAssetStateMachineTests
{
    private static VideoAsset ValidAsset() =>
        VideoAsset.Create(
            MediaData.Create(
                FileName.Create("movie.mp4").Value,
                ContentType.Create("video/mp4").Value,
                FileSize.Create(1000).Value,
                1).Value,
            MediaOwner.ForLesson(Guid.NewGuid()).Value).Value;

    private static StorageKey ValidStorageKey() =>
        StorageKey.Create("videos", "raw", Guid.NewGuid().ToString("N")).Value;

    [Fact]
    public void BeginUpload_FromPending_Succeeds()
    {
        var asset = ValidAsset();

        var result = asset.BeginUpload();

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(MediaStatus.UPLOADING);
    }

    [Fact]
    public void BeginUpload_FromNonPending_ReturnsError()
    {
        var asset = ValidAsset();
        asset.BeginUpload();

        var result = asset.BeginUpload();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.cannot.begin.upload");
    }

    [Fact]
    public void CompleteUpload_FromUploading_Succeeds()
    {
        var asset = ValidAsset();
        asset.BeginUpload();

        var result = asset.CompleteUpload(ValidStorageKey());

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(MediaStatus.READY);
    }

    [Fact]
    public void CompleteUpload_WithoutBeginUpload_ReturnsError()
    {
        var asset = ValidAsset();

        var result = asset.CompleteUpload(ValidStorageKey());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.cannot.complete.upload");
    }

    [Fact]
    public void MarkAsDeleted_FromPending_ReturnsError()
    {
        var asset = ValidAsset();

        var result = asset.MarkAsDeleted();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.cannot.delete.pending");
    }

    [Fact]
    public void MarkAsDeleted_FromReady_Succeeds()
    {
        var asset = ValidAsset();
        asset.BeginUpload();
        asset.CompleteUpload(ValidStorageKey());

        var result = asset.MarkAsDeleted();

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(MediaStatus.DELETED);
    }

    [Fact]
    public void MarkAsDeleted_Twice_SecondCallReturnsError()
    {
        var asset = ValidAsset();
        asset.BeginUpload();
        asset.CompleteUpload(ValidStorageKey());
        asset.MarkAsDeleted();

        var result = asset.MarkAsDeleted();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.already.deleted");
    }
}