using VSProjectRenamer.Core;

namespace VSProjectRenamer.Tests;

public class FileTypeFilterTests
{
    [Theory]
    [InlineData("Program.cs",          true)]
    [InlineData("app.csproj",          true)]
    [InlineData("MySolution.sln",      true)]
    [InlineData("appsettings.json",    true)]
    [InlineData("docker-compose.yml",  true)]
    [InlineData("Dockerfile",          true)]
    [InlineData("styles.scss",         true)]
    [InlineData("app.ts",              true)]
    [InlineData("README.md",           true)]
    [InlineData("script.sh",           true)]
    [InlineData("image.png",           false)]
    [InlineData("photo.jpg",           false)]
    [InlineData("binary.dll",          false)]
    [InlineData("archive.zip",         false)]
    [InlineData("video.mp4",           false)]
    public void ShouldProcessContents_ReturnsExpected(string fileName, bool expected)
    {
        Assert.Equal(expected, FileTypeFilter.ShouldProcessContents(fileName));
    }
}
