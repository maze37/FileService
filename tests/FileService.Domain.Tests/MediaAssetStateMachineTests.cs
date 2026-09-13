using FileService.Domain.Assets;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FileService.Domain.Tests;

public class MediaAssetStateMachineTests
{
    private static readonly DateTimeOffset TestTime = DateTimeOffset.UtcNow;

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
    
    private static StorageMetadata MatchingStorageMetadata() =>
        StorageMetadata.Create(
            eTag: "\"d41d8cd98f00b204e9800998ecf8427e\"",
            actualContentType: TestContentType,
            actualSizeBytes: TestFileSizeBytes).Value;

    [Fact]
    public void BeginUpload_FromPending_Succeeds()
    {
        var asset = ValidAsset();

        var result = asset.BeginUpload(TestTime);

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(MediaStatus.UPLOADING);
        asset.UpdatedWhen.Should().Be(TestTime);
    }

    [Fact]
    public void BeginUpload_FromNonPending_ReturnsError()
    {
        var asset = ValidAsset();
        asset.BeginUpload(TestTime);

        var result = asset.BeginUpload(TestTime);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.cannot.begin.upload");
    }

    [Fact]
    public void CompleteUpload_FromUploading_WithMatchingMetadata_Succeeds()
    {
        var asset = ValidAsset();
        asset.BeginUpload(TestTime);

        var result = asset.CompleteUpload(MatchingStorageMetadata(), TestTime);

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(MediaStatus.READY);
        asset.UpdatedWhen.Should().Be(TestTime);
        asset.StorageMetadata.Should().NotBeNull();
        asset.StorageMetadata!.ActualSizeBytes.Should().Be(TestFileSizeBytes);
    }

    [Fact]
    public void CompleteUpload_WithoutBeginUpload_ReturnsError()
    {
        var asset = ValidAsset();

        var result = asset.CompleteUpload(MatchingStorageMetadata(), TestTime);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.cannot.complete.upload");
    }

    [Fact]
    public void CompleteUpload_WithSizeMismatch_ReturnsConflictAndDoesNotChangeStatus()
    {
        var asset = ValidAsset();
        asset.BeginUpload(TestTime);

        var mismatchedMetadata = StorageMetadata.Create(
            eTag: "\"abc123\"",
            actualContentType: TestContentType,
            actualSizeBytes: TestFileSizeBytes + 1).Value;

        var result = asset.CompleteUpload(mismatchedMetadata, TestTime);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.size.mismatch");
        asset.Status.Should().Be(MediaStatus.UPLOADING);
    }

    [Fact]
    public void CompleteUpload_WithContentTypeMismatch_ReturnsConflict()
    {
        var asset = ValidAsset();
        asset.BeginUpload(TestTime);

        var mismatchedMetadata = StorageMetadata.Create(
            eTag: "\"abc123\"",
            actualContentType: "image/png",
            actualSizeBytes: TestFileSizeBytes).Value;

        var result = asset.CompleteUpload(mismatchedMetadata, TestTime);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.content_type.mismatch");
        asset.Status.Should().Be(MediaStatus.UPLOADING);
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
        asset.BeginUpload(TestTime);
        asset.CompleteUpload(MatchingStorageMetadata(), TestTime);

        var result = asset.MarkAsDeleted();

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(MediaStatus.DELETED);
    }

    [Fact]
    public void MarkAsDeleted_Twice_SecondCallReturnsError()
    {
        var asset = ValidAsset();
        asset.BeginUpload(TestTime);
        asset.CompleteUpload(MatchingStorageMetadata(), TestTime);
        asset.MarkAsDeleted();

        var result = asset.MarkAsDeleted();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.already.deleted");
    }
}