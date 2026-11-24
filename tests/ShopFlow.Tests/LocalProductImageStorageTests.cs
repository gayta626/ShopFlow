using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using ShopFlow.Api.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ShopFlow.Tests;

public class LocalProductImageStorageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "shopflow-image-test-" + Guid.NewGuid().ToString("N"));
    private LocalProductImageStorage Storage() => new(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ProductImages:StoragePath"] = root
        }).Build(), new TestEnvironment());

    [Theory]
    [InlineData(".png")]
    [InlineData(".jpeg")]
    public async Task SaveThenDelete_WritesDecodableImageWithGeneratedName(string extension)
    {
        using var image = new Image<Rgba32>(8, 6);
        using var source = new MemoryStream();
        if (extension == ".png") await image.SaveAsPngAsync(source);
        else await image.SaveAsJpegAsync(source);
        source.Position = 0;
        var storage = Storage();
        var name = await storage.SaveAsync(new FormFile(source, 0, source.Length, "file", "original" + extension), default);
        Assert.True(Guid.TryParseExact(Path.GetFileNameWithoutExtension(name), "N", out _));
        Assert.Equal(extension == ".jpeg" ? ".jpg" : extension, Path.GetExtension(name));
        using (var saved = await Image.LoadAsync(Path.Combine(root, name)))
        {
            Assert.Equal(8, saved.Width);
            Assert.Equal(6, saved.Height);
        }
        await storage.DeleteAsync(name, default);
        await storage.DeleteAsync(name, default);
        Assert.Empty(Directory.GetFiles(root));
    }

    [Theory]
    [InlineData(8, "wrong.jpg")]
    [InlineData(4097, "wide.png")]
    public async Task Save_RejectsMismatchedFormatOrOversizeDimensions(int width, string fileName)
    {
        using var image = new Image<Rgba32>(width, 1);
        using var source = new MemoryStream();
        await image.SaveAsPngAsync(source);
        source.Position = 0;
        await Assert.ThrowsAsync<ArgumentException>(() => Storage().SaveAsync(new FormFile(source, 0, source.Length, "file", fileName), default));
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public async Task Save_RejectsNonImage()
    {
        using var source = new MemoryStream("not an image"u8.ToArray());
        await Assert.ThrowsAsync<ArgumentException>(() => Storage().SaveAsync(new FormFile(source, 0, source.Length, "file", "fake.png"), default));
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public async Task CancelledSave_DoesNotWriteFile()
    {
        using var source = new MemoryStream(new byte[1]);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Storage().SaveAsync(new FormFile(source, 0, 1, "file", "test.png"), new CancellationToken(true)));
        Assert.False(Directory.Exists(root));
    }

    [Theory]
    [InlineData("../outside.png")]
    [InlineData("..\\outside.png")]
    [InlineData("normal.png")]
    public async Task Delete_RejectsInvalidNames(string name)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Storage().DeleteAsync(name, default));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ShopFlow.Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
