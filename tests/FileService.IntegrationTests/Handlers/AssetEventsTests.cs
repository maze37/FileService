using FileService.Core.Abstractions;
using FileService.Domain.Enums;
using FileService.Infrastructure.Postgres;
using FileService.IntegrationTests.Infrastructure;
using IntegrationEvents.Files.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FileService.IntegrationTests.Handlers;

public class AssetEventsTests : FileServiceBaseTests
{
    private readonly IntegrationTestsWebFactory factory;

    public AssetEventsTests(IntegrationTestsWebFactory factory) : base(factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task CompleteUpload_PublishesAssetReadyWithOwnerAndAssetType()
    {
        var entityId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        var assetId = await UploadFile(RandomBytes(KB), entityId);
        var message = await factory.Broker.WaitForAsync("asset.ready.user", assetId);

        Assert.Equal(entityId, message.GetProperty("entityId").GetGuid());
        Assert.Equal("user", message.GetProperty("entityType").GetString());
        Assert.Equal("VIDEO", message.GetProperty("assetType").GetString());
        Assert.InRange(message.GetProperty("occurredAt").GetDateTimeOffset(), before, DateTimeOffset.UtcNow);
        Assert.Equal(MediaStatus.UPLOADED, (await GetAsset(assetId)).Status);
    }

    [Fact]
    public async Task DeleteFile_PublishesAssetDeleted()
    {
        var entityId = Guid.NewGuid();
        var assetId = await UploadFile(RandomBytes(KB), entityId);
        await factory.Broker.WaitForAsync("asset.ready.user", assetId);

        var response = await DeleteFile(assetId);

        Assert.True(response.IsSuccessStatusCode);
        var message = await factory.Broker.WaitForAsync("asset.deleted.user", assetId);
        Assert.Equal(entityId, message.GetProperty("entityId").GetGuid());
        Assert.Equal("VIDEO", message.GetProperty("assetType").GetString());
        Assert.Equal(MediaStatus.DELETED, (await GetAsset(assetId)).Status);
    }

    [Fact]
    public async Task TransactionWithoutCommit_RollsBackAssetAndOutboxMessage()
    {
        var entityId = Guid.NewGuid();
        var init = await InitiateUpload(RandomBytes(KB), entityId);
        await using (var scope = Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FileServiceDbContext>();
            var transaction = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxService>();
            Assert.True((await transaction.BeginTransactionAsync()).IsSuccess);
            var asset = await db.MediaAssets.SingleAsync(x => x.Id == init.AssetId);
            Assert.True(asset.MarkUploaded().IsSuccess);
            await outbox.PublishAsync(new AssetReady(init.AssetId, entityId, "user", "VIDEO", DateTimeOffset.UtcNow));
            Assert.True((await transaction.SaveChangesAsync()).IsSuccess);
            Assert.Equal(1, await PendingMessages(db, init.AssetId));
            // Dispose без Commit: откатываются и бизнес-данные, и сообщение.
        }

        Assert.Equal(MediaStatus.UPLOADING, (await GetAsset(init.AssetId)).Status);
        await ExecuteInDb(async db => Assert.Equal(0, await PendingMessages(db, init.AssetId)));
    }

    [Fact]
    public async Task CompleteUpload_WhenRabbitMqUnavailable_PersistsEventAndDeliversAfterRecovery()
    {
        var data = RandomBytes(KB);
        var init = await InitiateUpload(data);
        (await PutToUrl(init.UploadUrl, data)).EnsureSuccessStatusCode();

        await factory.Broker.StopApplicationAsync();
        try
        {
            var response = await CompleteUpload(init.AssetId);
            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal(MediaStatus.UPLOADED, (await GetAsset(init.AssetId)).Status);
            await ExecuteInDb(async db => Assert.Equal(1, await PendingMessages(db, init.AssetId)));
        }
        finally
        {
            await factory.Broker.StartApplicationAsync();
        }

        await factory.Broker.WaitForAsync("asset.ready.user", init.AssetId);
        var deadline = DateTime.UtcNow.AddSeconds(15);
        int pending = -1;
        do
        {
            await ExecuteInDb(async db => pending = await PendingMessages(db, init.AssetId));
            if (pending == 0) break;
            await Task.Delay(100);
        } while (DateTime.UtcNow < deadline);
        Assert.Equal(0, pending);
    }

    private static Task<int> PendingMessages(FileServiceDbContext db, Guid assetId) =>
        db.Database.SqlQuery<int>($"""
            SELECT count(*)::int AS "Value"
            FROM files.wolverine_outgoing_envelopes
            WHERE position(convert_to({assetId.ToString()}, 'UTF8') in body) > 0
            """).SingleAsync();
}
