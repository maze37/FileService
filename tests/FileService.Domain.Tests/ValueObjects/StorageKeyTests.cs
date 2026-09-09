using FileService.Domain.ValueObjects;
using FluentAssertions;

namespace FileService.Domain.Tests.ValueObjects;

public class StorageKeyTests
{
    [Theory]
    [InlineData("", "prefix", "key")]
    [InlineData("bucket", "prefix", "")]
    public void Create_MissingRequiredParts_ReturnsError(string bucket, string prefix, string key)
    {
        var result = StorageKey.Create(bucket, prefix, key);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("bucket", "../etc", "key")]
    [InlineData("bucket", "prefix", "../../secret")]
    [InlineData("bucket", "prefix", "key\\with\\backslash")]
    [InlineData("bucket", "/prefix", "key")]
    public void Create_PathTraversalOrInvalidChars_ReturnsError(string bucket, string prefix, string key)
    {
        var result = StorageKey.Create(bucket, prefix, key);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Valid_BuildsCorrectPaths()
    {
        var result = StorageKey.Create("videos", "raw", "abc123");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("raw/abc123");
        result.Value.FullPath.Should().Be("videos/raw/abc123");
    }

    [Fact]
    public void CreateNew_GeneratesGuidBasedKey()
    {
        var result = StorageKey.CreateNew("videos", "raw");

        result.IsSuccess.Should().BeTrue();
        result.Value.Key.Should().NotBeNullOrWhiteSpace();
    }
}