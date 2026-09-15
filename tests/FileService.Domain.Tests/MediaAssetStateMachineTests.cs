using FileService.Domain.Assets;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FileService.Domain.Tests;

public class MediaAssetStateMachineTests
{
    private const long TestFileSizeBytes = 1000;
    private const string TestContentType = "video/mp4";

    private static StorageKey ValidStorageKey() =>
        StorageKey.Create("videos", "raw", Guid.NewGuid().ToString("N")).Value;

    private static VideoAsset ValidAsset() =>
        VideoAsset.Create(
            MediaData.Create(
                FileName.Create("movie.mp4").Value,
                ContentType.Create(TestContentType).Value,
                FileSize.Create(TestFileSizeBytes).Value,
                expectedChunksCount: 1).Value,
            MediaOwner.ForLesson(Guid.NewGuid()).Value,
            ValidStorageKey()).Value;

    [Fact]
    public void BeginUpload_FromPending_Succeeds()
    {
        var asset = ValidAsset();

        var result = asset.BeginUpload();

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(MediaStatus.UPLOADING);
        asset.UpdatedWhen.Should().NotBeNull();
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
    public void AttachUploadId_FromUploading_Succeeds()
    {
        var asset = ValidAsset();
        asset.BeginUpload();

        var result = asset.AttachUploadId("upload-123");

        result.IsSuccess.Should().BeTrue();
        asset.UploadId.Should().Be("upload-123");
    }

    [Fact]
    public void AttachUploadId_FromPending_ReturnsError()
    {
        var asset = ValidAsset();

        var result = asset.AttachUploadId("upload-123");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.invalid_status");
        asset.UploadId.Should().BeNull();
    }

    [Fact]
    public void MarkUploaded_FromUploading_Succeeds()
    {
        var asset = ValidAsset();
        asset.BeginUpload();

        var result = asset.MarkUploaded();

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(MediaStatus.UPLOADED);
    }

    [Fact]
    public void MarkUploaded_WithoutBeginUpload_ReturnsError()
    {
        var asset = ValidAsset();

        var result = asset.MarkUploaded();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.cannot.complete.upload");
        asset.Status.Should().Be(MediaStatus.PENDING);
    }

    [Fact]
    public void MarkUploaded_Twice_SecondCallReturnsError()
    {
        var asset = ValidAsset();
        asset.BeginUpload();
        asset.MarkUploaded();

        var result = asset.MarkUploaded();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.cannot.complete.upload");
        asset.Status.Should().Be(MediaStatus.UPLOADED);
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
    public void MarkAsDeleted_FromUploaded_Succeeds()
    {
        var asset = ValidAsset();
        asset.BeginUpload();
        asset.MarkUploaded();

        var result = asset.MarkAsDeleted();

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(MediaStatus.DELETED);
    }

    [Fact]
    public void MarkAsDeleted_Twice_SecondCallReturnsError()
    {
        var asset = ValidAsset();
        asset.BeginUpload();
        asset.MarkUploaded();
        asset.MarkAsDeleted();

        var result = asset.MarkAsDeleted();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.already.deleted");
    }

    [Fact]
    public void MarkAsCancelled_FromPending_ReturnsError()
    {
        var asset = ValidAsset();

        var result = asset.MarkAsCancelled();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.cannot.cancel.pending");
    }

    [Fact]
    public void MarkAsCancelled_FromUploading_Succeeds()
    {
        var asset = ValidAsset();
        asset.BeginUpload();

        var result = asset.MarkAsCancelled();

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(MediaStatus.CANCELLED);
    }

    [Fact]
    public void MarkAsCancelled_Twice_SecondCallReturnsError()
    {
        var asset = ValidAsset();
        asset.BeginUpload();
        asset.MarkAsCancelled();

        var result = asset.MarkAsCancelled();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.already.cancelled");
    }
}