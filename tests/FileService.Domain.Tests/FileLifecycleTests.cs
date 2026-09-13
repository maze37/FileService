using Core.Abstractions;
using Core.Database;
using CSharpFunctionalExtensions;
using FileService.Core.Abstractions;
using FileService.Core.UseCases.Commands.CancelUpload;
using FileService.Core.UseCases.Commands.DeleteFile;
using FileService.Core.UseCases.Queries.GetFilesByTargetEntity;
using FileService.Domain.Assets;
using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Result;

namespace FileService.Domain.Tests;

public class FileLifecycleTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static VideoAsset NewPendingAsset()
    {
        var fileName = FileName.Create("movie.mp4").Value;
        var contentType = ContentType.Create("video/mp4").Value;
        var fileSize = FileSize.Create(1000).Value;
        var mediaData = MediaData.Create(fileName, contentType, fileSize, expectedChunksCount: 1).Value;
        var owner = MediaOwner.ForUser(Guid.NewGuid()).Value;
        var storageKey = StorageKey.CreateNew(VideoAsset.BUCKET, "user").Value;

        return VideoAsset.Create(mediaData, owner, storageKey).Value;
    }

    private static VideoAsset NewUploadingAsset()
    {
        var asset = NewPendingAsset();
        asset.BeginUpload(Now);
        return asset;
    }

    private static VideoAsset NewReadyAsset()
    {
        var asset = NewUploadingAsset();
        var meta = StorageMetadata.Create("etag", "video/mp4", 1000).Value;
        asset.CompleteUpload(meta, Now);
        return asset;
    }

    private static Mock<ITransactionManager> HappyTransactionManager()
    {
        var scope = new Mock<ITransactionScope>();
        scope.Setup(x => x.Commit()).Returns(UnitResult.Success<Error>());

        var tm = new Mock<ITransactionManager>();
        tm.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>(), null))
          .ReturnsAsync(Result.Success<ITransactionScope, Error>(scope.Object));
        tm.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
          .ReturnsAsync(UnitResult.Success<Error>());

        return tm;
    }
    
    [Fact]
    public async Task CancelUpload_FromPending_ReturnsConflict()
    {
        var asset = NewPendingAsset();

        var repo = new Mock<IMediaAssetRepository>();
        repo.Setup(x => x.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<MediaAsset, Error>(asset));

        var handler = new CancelUploadHandler(
            HappyTransactionManager().Object,
            Mock.Of<IDateTimeProvider>(),
            Mock.Of<ILogger<CancelUploadHandler>>(),
            repo.Object,
            Mock.Of<IS3Provider>());

        var result = await handler.HandleAsync(new CancelUploadCommand(asset.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.cannot.cancel.pending");
        asset.Status.Should().Be(MediaStatus.PENDING);
    }

    [Fact]
    public async Task CancelUpload_FromUploading_Succeeds()
    {
        var asset = NewUploadingAsset();

        var repo = new Mock<IMediaAssetRepository>();
        repo.Setup(x => x.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<MediaAsset, Error>(asset));

        var s3 = new Mock<IS3Provider>();
        s3.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(UnitResult.Success<Error>());

        var handler = new CancelUploadHandler(
            HappyTransactionManager().Object,
            Mock.Of<IDateTimeProvider>(),
            Mock.Of<ILogger<CancelUploadHandler>>(),
            repo.Object,
            s3.Object);

        var result = await handler.HandleAsync(new CancelUploadCommand(asset.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(MediaStatus.CANCELLED);
    }
    
    [Fact]
    public async Task CancelUpload_Twice_SecondCallReturnsConflict()
    {
        var asset = NewUploadingAsset();

        var repo = new Mock<IMediaAssetRepository>();
        repo.Setup(x => x.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<MediaAsset, Error>(asset));

        var s3 = new Mock<IS3Provider>();
        s3.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(UnitResult.Success<Error>());

        var handler = new CancelUploadHandler(
            HappyTransactionManager().Object,
            Mock.Of<IDateTimeProvider>(),
            Mock.Of<ILogger<CancelUploadHandler>>(),
            repo.Object,
            s3.Object);

        var first = await handler.HandleAsync(new CancelUploadCommand(asset.Id), CancellationToken.None);
        first.IsSuccess.Should().BeTrue();

        var second = await handler.HandleAsync(new CancelUploadCommand(asset.Id), CancellationToken.None);

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("media.asset.already.cancelled");
    }

    [Fact]
    public async Task CancelUpload_AssetNotFound_ReturnsError()
    {
        var missingId = Guid.NewGuid();

        var repo = new Mock<IMediaAssetRepository>();
        repo.Setup(x => x.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<MediaAsset, Error>(
                Error.NotFound("media.asset.not_found", "Asset не найден")));

        var handler = new CancelUploadHandler(
            Mock.Of<ITransactionManager>(),
            Mock.Of<IDateTimeProvider>(),
            Mock.Of<ILogger<CancelUploadHandler>>(),
            repo.Object,
            Mock.Of<IS3Provider>());

        var result = await handler.HandleAsync(new CancelUploadCommand(missingId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.not_found");
    }

    [Fact]
    public async Task CancelUpload_S3CleanupFails_StillSucceeds()
    {
        var asset = NewUploadingAsset();

        var repo = new Mock<IMediaAssetRepository>();
        repo.Setup(x => x.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<MediaAsset, Error>(asset));

        var s3 = new Mock<IS3Provider>();
        s3.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(Error.Failure("s3.unavailable", "недоступен"));

        var handler = new CancelUploadHandler(
            HappyTransactionManager().Object,
            Mock.Of<IDateTimeProvider>(),
            Mock.Of<ILogger<CancelUploadHandler>>(),
            repo.Object,
            s3.Object);

        var result = await handler.HandleAsync(new CancelUploadCommand(asset.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue("сбой S3 не должен ломать уже завершённую в БД операцию");
        asset.Status.Should().Be(MediaStatus.CANCELLED);
    }
    

    [Fact]
    public async Task DeleteFile_FromReady_Succeeds()
    {
        var asset = NewReadyAsset();

        var repo = new Mock<IMediaAssetRepository>();
        repo.Setup(x => x.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<MediaAsset, Error>(asset));

        var s3 = new Mock<IS3Provider>();
        s3.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(UnitResult.Success<Error>());

        var handler = new DeleteFileHandler(
            HappyTransactionManager().Object,
            Mock.Of<IDateTimeProvider>(),
            Mock.Of<ILogger<DeleteFileHandler>>(),
            repo.Object,
            s3.Object);

        var result = await handler.HandleAsync(new DeleteFileCommand(asset.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        asset.Status.Should().Be(MediaStatus.DELETED);
    }

    [Fact]
    public async Task DeleteFile_Twice_SecondCallReturnsConflict()
    {
        var asset = NewReadyAsset();

        var repo = new Mock<IMediaAssetRepository>();
        repo.Setup(x => x.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<MediaAsset, Error>(asset));

        var s3 = new Mock<IS3Provider>();
        s3.Setup(x => x.DeleteObjectAsync(It.IsAny<string>(), It.IsAny<StorageKey>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(UnitResult.Success<Error>());

        var handler = new DeleteFileHandler(
            HappyTransactionManager().Object,
            Mock.Of<IDateTimeProvider>(),
            Mock.Of<ILogger<DeleteFileHandler>>(),
            repo.Object,
            s3.Object);

        var first = await handler.HandleAsync(new DeleteFileCommand(asset.Id), CancellationToken.None);
        first.IsSuccess.Should().BeTrue();

        var second = await handler.HandleAsync(new DeleteFileCommand(asset.Id), CancellationToken.None);

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("media.asset.cannot.delete");
    }

    [Fact]
    public async Task DeleteFile_PendingAsset_ReturnsConflict()
    {
        var asset = NewPendingAsset();

        var repo = new Mock<IMediaAssetRepository>();
        repo.Setup(x => x.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<MediaAsset, Error>(asset));

        var handler = new DeleteFileHandler(
            Mock.Of<ITransactionManager>(),
            Mock.Of<IDateTimeProvider>(),
            Mock.Of<ILogger<DeleteFileHandler>>(),
            repo.Object,
            Mock.Of<IS3Provider>());

        var result = await handler.HandleAsync(new DeleteFileCommand(asset.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        asset.Status.Should().Be(MediaStatus.PENDING);
    }

    [Fact]
    public async Task DeleteFile_AssetNotFound_ReturnsError()
    {
        var missingId = Guid.NewGuid();

        var repo = new Mock<IMediaAssetRepository>();
        repo.Setup(x => x.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<MediaAsset, Error>(
                Error.NotFound("media.asset.not_found", "Asset не найден")));

        var handler = new DeleteFileHandler(
            Mock.Of<ITransactionManager>(),
            Mock.Of<IDateTimeProvider>(),
            Mock.Of<ILogger<DeleteFileHandler>>(),
            repo.Object,
            Mock.Of<IS3Provider>());

        var result = await handler.HandleAsync(new DeleteFileCommand(missingId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.asset.not_found");
    }
    

    [Fact]
    public async Task GetFilesByTargetEntity_ExcludesDeletedFiles()
    {
        var entityId = Guid.NewGuid();

        var ready = NewReadyAsset();
        var deleted = NewReadyAsset();
        deleted.MarkAsDeleted();

        var repo = new Mock<IMediaAssetRepository>();
        repo.Setup(x => x.GetByOwnerAsync("user", entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<MediaAsset>)new List<MediaAsset> { ready, deleted });

        var handler = new GetFilesByTargetEntityHandler(repo.Object);

        var result = await handler.HandleAsync(
            new GetFilesByTargetEntityQuery("user", entityId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().ContainSingle();
        result.Value.Files.Select(f => f.Id).Should().NotContain(deleted.Id);
    }

    [Fact]
    public async Task GetFilesByTargetEntity_UnknownContext_ReturnsValidationError()
    {
        var handler = new GetFilesByTargetEntityHandler(Mock.Of<IMediaAssetRepository>());

        var result = await handler.HandleAsync(
            new GetFilesByTargetEntityQuery("not-a-context", Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("media.owner.context.invalid");
    }

    [Fact]
    public async Task GetFilesByTargetEntity_EmptyList_WhenNoAssets()
    {
        var entityId = Guid.NewGuid();

        var repo = new Mock<IMediaAssetRepository>();
        repo.Setup(x => x.GetByOwnerAsync("user", entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<MediaAsset>)Array.Empty<MediaAsset>());

        var handler = new GetFilesByTargetEntityHandler(repo.Object);

        var result = await handler.HandleAsync(
            new GetFilesByTargetEntityQuery("user", entityId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Files.Should().BeEmpty();
    }
}