using CSharpFunctionalExtensions;
using FileService.Contracts;
using FileService.Core.HttpCommunication;
using FileService.IntegrationTests.Infrastructure;
using SharedKernel;

namespace FileService.IntegrationTests.Handlers;

public class GetFilesByTargetEntityTests : FileServiceBaseTests
{
    public GetFilesByTargetEntityTests(IntegrationTestsWebFactory factory) : base(factory) { }

    [Fact]
    public async Task Returns_OnlyFilesOfRequestedEntity()
    {
        Guid entityA = Guid.NewGuid();
        Guid entityB = Guid.NewGuid();

        Guid a1 = await UploadFile(RandomBytes(KB), entityA);
        Guid a2 = await UploadFile(RandomBytes(KB), entityA);
        Guid b1 = await UploadFile(RandomBytes(KB), entityB);

        Assert.Equal(new[] { a1, a2 }.Order(), (await GetFileIds(entityA)).Order());
        Assert.Equal(new[] { b1 }, await GetFileIds(entityB));
    }

    [Fact]
    public async Task DeletedFile_DisappearsFromList()
    {
        Guid entity = Guid.NewGuid();
        Guid keep = await UploadFile(RandomBytes(KB), entity);
        Guid remove = await UploadFile(RandomBytes(KB), entity);

        await DeleteFile(remove);

        Assert.Equal(new[] { keep }, await GetFileIds(entity));
    }

    [Fact]
    public async Task EntityWithoutFiles_ReturnsEmptyList()
    {
        Assert.Empty(await GetFileIds(Guid.NewGuid()));
    }

    [Fact]
    public async Task UnknownContext_Fails()
    {
        var response = await AppHttpClient.GetAsync($"api/files?context=no-such-context&entityId={Guid.NewGuid()}");

        Assert.False(response.IsSuccessStatusCode);
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