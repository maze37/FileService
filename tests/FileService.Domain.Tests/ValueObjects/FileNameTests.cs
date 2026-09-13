using FileService.Domain.ValueObjects;
using FluentAssertions;

namespace FileService.Domain.Tests.ValueObjects;

public class FileNameTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_ReturnsError(string? input)
    {
        var result = FileName.Create(input);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_NoExtension_ReturnsError()
    {
        var result = FileName.Create("filename");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_DotAtTheEnd_ReturnsError()
    {
        var result = FileName.Create("filename.");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_TooLongName_ReturnsError()
    {
        var longName = new string('a', 300) + ".txt";

        var result = FileName.Create(longName);

        result.IsFailure.Should().BeTrue();
    }
}