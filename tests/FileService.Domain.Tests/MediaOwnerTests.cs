using FluentAssertions;

namespace FileService.Domain.Tests;

public class MediaOwnerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown-context")]
    public void Create_InvalidContext_ReturnsError(string? context)
    {
        var result = MediaOwner.Create(context!, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_EmptyEntityId_ReturnsError()
    {
        var result = MediaOwner.Create("lesson", Guid.Empty);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_ValidContext_NormalizesToLowercase()
    {
        var result = MediaOwner.Create("  LESSON  ", Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Value.Context.Should().Be("lesson");
    }

    [Fact]
    public void ForLesson_CreatesLessonOwner()
    {
        var lessonId = Guid.NewGuid();

        var result = MediaOwner.ForLesson(lessonId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Context.Should().Be("lesson");
        result.Value.EntityId.Should().Be(lessonId);
    }
}