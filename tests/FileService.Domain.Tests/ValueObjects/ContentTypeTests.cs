using FileService.Domain.Enums;
using FileService.Domain.ValueObjects;
using FluentAssertions;

namespace FileService.Domain.Tests.ValueObjects;

public class ContentTypeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_Empty_ReturnsError(string? input)
    {
        var result = ContentType.Create(input!);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData("video/mp4", Category.VIDEO)]
    [InlineData("audio/mpeg", Category.AUDIO)]
    [InlineData("image/png", Category.IMAGE)]
    [InlineData("application/document-pdf", Category.DOCUMENT)]
    [InlineData("application/octet-stream", Category.UNKNOWN)]
    public void Create_ResolvesCategory(string input, Category expected)
    {
        var result = ContentType.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Should().Be(expected);
    }
}