using FileService.Domain.Assets;
using FileService.Domain.Enums;
using FileService.IntegrationTests.Infrastructure;

namespace FileService.IntegrationTests.Handlers;

public class CancelUploadTests : FileServiceBaseTests
{
    public CancelUploadTests(IntegrationTestsWebFactory factory) : base(factory) { }

    [Fact]
    public async Task Cancel_UploadInProgress_MarksCancelledAndRemovesObjectFromStorage()
    {
        byte[] data = RandomBytes(KB);
        var init = await InitiateUpload(data);
        await PutToUrl(init.UploadUrl, data);

        MediaAsset asset = await GetAsset(init.AssetId);
        Assert.True(await ObjectExists(asset));

        var response = await CancelUpload(init.AssetId);

        Assert.True(response.IsSuccessStatusCode);
        asset = await GetAsset(init.AssetId);
        Assert.Equal(MediaStatus.CANCELLED, asset.Status);
        Assert.False(await ObjectExists(asset));
    }

    [Fact]
    public async Task Cancel_WhenNothingWasUploaded_StillCancels()
    {
        var init = await InitiateUpload(RandomBytes(KB));

        var response = await CancelUpload(init.AssetId);

        Assert.True(response.IsSuccessStatusCode);
        MediaAsset asset = await GetAsset(init.AssetId);
        Assert.Equal(MediaStatus.CANCELLED, asset.Status);
        Assert.False(await ObjectExists(asset));
    }

    [Fact]
    public async Task Cancel_Twice_SecondCallFails()
    {
        var init = await InitiateUpload(RandomBytes(KB));
        Assert.True((await CancelUpload(init.AssetId)).IsSuccessStatusCode);

        var second = await CancelUpload(init.AssetId);

        Assert.False(second.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Cancel_UnknownAsset_Fails()
    {
        var response = await CancelUpload(Guid.NewGuid());

        Assert.False(response.IsSuccessStatusCode);
    }
}