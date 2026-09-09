using FileService.Domain.ValueObjects;
using FluentAssertions;

namespace FileService.Domain.Tests.ValueObjects;

public class FileSizeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonPositive_ReturnsError(long bytes)
    {
        var result = FileSize.Create(bytes);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_ExceedsMax_ReturnsError()
    {
        var result = FileSize.Create(FileSize.MAX_BYTES + 1);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_ValidSize_Succeeds()
    {
        var result = FileSize.Create(1024);

        result.IsSuccess.Should().BeTrue();
        result.Value.Bytes.Should().Be(1024);
    }
}