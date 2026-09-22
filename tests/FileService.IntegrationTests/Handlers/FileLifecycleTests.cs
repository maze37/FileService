using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Contracts.Dtos;
using FileService.Domain.Assets;
using FileService.Domain.Enums;
using FileService.IntegrationTests.Infrastructure;
using SharedKernel;

namespace FileService.IntegrationTests.Handlers;

public class FileLifecycleTests : FileServiceBaseTests
{
    public FileLifecycleTests(IntegrationTestsWebFactory factory) : base(factory) { }

    [Fact]
    public async Task Cancel_PendingUpload_MarksCancelledAndRemovesObject()
    {
        byte[] data = RandomBytes(KB);
        var init = await InitiateUpload(data);
        await PutToUrl(init.UploadUrl, data);

        MediaAsset asset = await GetAsset(init.AssetId);
        Assert.True(await ObjectExists(asset));

        var response = await AppHttpClient.PatchAsync($"api/files/{init.AssetId}/cancel", null);

        Assert.True(response.IsSuccessStatusCode);
        asset = await GetAsset(init.AssetId);
        Assert.Equal(MediaStatus.CANCELLED, asset.Status);
        Assert.False(await ObjectExists(asset));
    }

    [Fact]
    public async Task Delete_UploadedFile_MarksDeletedAndRemovesObject()
    {
        Guid id = await UploadFile(RandomBytes(KB));

        MediaAsset asset = await GetAsset(id);
        Assert.True(await ObjectExists(asset));

        var response = await AppHttpClient.DeleteAsync($"api/files/{id}");

        Assert.True(response.IsSuccessStatusCode);
        asset = await GetAsset(id);
        Assert.Equal(MediaStatus.DELETED, asset.Status);
        Assert.False(await ObjectExists(asset));
    }

    [Fact]
    public async Task Delete_FileThatIsStillUploading_Fails()
    {
        var init = await InitiateUpload(RandomBytes(KB));

        var response = await AppHttpClient.DeleteAsync($"api/files/{init.AssetId}");

        Assert.False(response.IsSuccessStatusCode);
        MediaAsset asset = await GetAsset(init.AssetId);
        Assert.Equal(MediaStatus.UPLOADING, asset.Status);
    }

    [Fact]
    public async Task GetFilesByTargetEntity_ReturnsOnlyOwnFiles_AndHidesDeleted()
    {
        Guid entityA = Guid.NewGuid();
        Guid entityB = Guid.NewGuid();

        Guid a1 = await UploadFile(RandomBytes(KB), entityA);
        Guid a2 = await UploadFile(RandomBytes(KB), entityA);
        Guid b1 = await UploadFile(RandomBytes(KB), entityB);
        
        Assert.Equal(new[] { a1, a2 }.Order(), (await GetFileIds(entityA)).Order());
        Assert.Equal(new[] { b1 }, await GetFileIds(entityB));

        await AppHttpClient.DeleteAsync($"api/files/{a1}");

        Assert.Equal(new[] { a2 }, await GetFileIds(entityA));
    }

    private async Task<List<Guid>> GetFileIds(Guid entityId)
    {
        var response = await AppHttpClient.GetAsync($"api/files?context=user&entityId={entityId}");
        Result<GetFilesByTargetEntityResponse, Error> result =
            await response.HandleResponseAsync<GetFilesByTargetEntityResponse>();

        Assert.True(result.IsSuccess);
        return result.Value.Files.Select(f => f.Id).ToList();
    }
}