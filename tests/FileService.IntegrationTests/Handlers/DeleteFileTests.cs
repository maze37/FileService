using FileService.Domain.Assets;
using FileService.Domain.Enums;
using FileService.IntegrationTests.Infrastructure;

namespace FileService.IntegrationTests.Handlers;

public class DeleteFileTests : FileServiceBaseTests
{
    public DeleteFileTests(IntegrationTestsWebFactory factory) : base(factory) { }

    [Fact]
    public async Task Delete_UploadedFile_MarksDeletedAndRemovesObjectFromStorage()
    {
        Guid id = await UploadFile(RandomBytes(KB));
        MediaAsset asset = await GetAsset(id);
        Assert.True(await ObjectExists(asset));

        var response = await DeleteFile(id);

        Assert.True(response.IsSuccessStatusCode);
        asset = await GetAsset(id);
        Assert.Equal(MediaStatus.DELETED, asset.Status);
        Assert.False(await ObjectExists(asset));
    }

    [Fact]
    public async Task GetFile_AfterDelete_Fails()
    {
        Guid id = await UploadFile(RandomBytes(KB));
        await DeleteFile(id);

        var response = await GetFile(id);

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Delete_Twice_SecondCallFails()
    {
        Guid id = await UploadFile(RandomBytes(KB));
        Assert.True((await DeleteFile(id)).IsSuccessStatusCode);

        var second = await DeleteFile(id);

        Assert.False(second.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Delete_FileThatIsStillUploading_FailsAndStatusUnchanged()
    {
        var init = await InitiateUpload(RandomBytes(KB));

        var response = await DeleteFile(init.AssetId);

        Assert.False(response.IsSuccessStatusCode);
        MediaAsset asset = await GetAsset(init.AssetId);
        Assert.Equal(MediaStatus.UPLOADING, asset.Status);
    }

    [Fact]
    public async Task Delete_UnknownFile_Fails()
    {
        var response = await DeleteFile(Guid.NewGuid());

        Assert.False(response.IsSuccessStatusCode);
    }
}