using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace ShopFlow.Api.Services;

public class LocalProductImageStorage : IProductImageStorage
{
    private const int MaxFileSize = 5 * 1024 * 1024;
    private const int MaxDimension = 4096;
    private readonly string _storagePath;

    public LocalProductImageStorage(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredPath = configuration["ProductImages:StoragePath"];
        if (string.IsNullOrWhiteSpace(configuredPath))
            throw new InvalidOperationException("Chưa cấu hình thư mục lưu ảnh.");

        _storagePath = Path.GetFullPath(configuredPath, environment.ContentRootPath);
    }
    //nhận file ảnh được upload, kiểm tra rồi lưu ảnh lên ổ đĩa
    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(file);
        if (file.Length <= 0)
            throw new ArgumentException("Ảnh rỗng.", nameof(file));
        if (file.Length > MaxFileSize)
            throw new ArgumentException("Ảnh tải lên không được quá 5 MB.", nameof(file));

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
            throw new ArgumentException("Chỉ nhận file .jpg, .jpeg hoặc .png.", nameof(file));

        // Đọc có giới hạn để các bước kiểm tra và giải mã dùng cùng một nội dung.
        using var content = new MemoryStream();
        using (var source = file.OpenReadStream())
        {
            var buffer = new byte[81920];
            int count;
            while ((count = await source.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
            {
                if (content.Length + count > MaxFileSize)
                    throw new ArgumentException("Ảnh tải lên không được quá 5 MB.", nameof(file));
                await content.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
            }
        }
        if (content.Length == 0)
            throw new ArgumentException("Ảnh rỗng.", nameof(file));

        var options = new DecoderOptions { MaxFrames = 1, SkipMetadata = true };
        Image image;
        string outputExtension;
        try
        {
            content.Position = 0;
            var info = await Image.IdentifyAsync(options, content, cancellationToken);
            if (info.Width < 1 || info.Height < 1 || info.Width > MaxDimension || info.Height > MaxDimension)
                throw new ArgumentException("Mỗi chiều ảnh phải từ 1 đến 4096 pixel.", nameof(file));

            var formatName = info.Metadata.DecodedImageFormat?.Name;
            if (formatName == "JPEG" && (extension == ".jpg" || extension == ".jpeg"))
                outputExtension = ".jpg";
            else if (formatName == "PNG" && extension == ".png")
                outputExtension = ".png";
            else
                throw new ArgumentException("Đuôi file phải khớp với nội dung ảnh JPEG hoặc PNG.", nameof(file));

            content.Position = 0;
            image = await Image.LoadAsync(options, content, cancellationToken);
        }
        catch (UnknownImageFormatException ex)
        {
            throw new ArgumentException("Không nhận diện được định dạng ảnh.", nameof(file), ex);
        }
        catch (InvalidImageContentException ex)
        {
            throw new ArgumentException("Nội dung ảnh không hợp lệ hoặc đã bị hỏng.", nameof(file), ex);
        }

        using (image)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(_storagePath);
            var fileName = $"{Guid.NewGuid():N}{outputExtension}";
            var path = Path.Combine(_storagePath, fileName);
            var created = false;
            try
            {
                // Chỉ dọn file khi chính lần gọi này đã tạo được nó.
                await using (var destination = new FileStream(path, FileMode.CreateNew,
                    FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    created = true;
                    IImageEncoder encoder = outputExtension == ".jpg"
                        ? new JpegEncoder { Quality = 90, SkipMetadata = true }
                        : new PngEncoder { SkipMetadata = true };
                    await image.SaveAsync(destination, encoder, cancellationToken);
                    await destination.FlushAsync(cancellationToken);
                }
                cancellationToken.ThrowIfCancellationRequested();
                return fileName;
            }
            catch (Exception saveError)
            {
                if (created)
                {
                    try
                    {
                        // Không dùng token đã hủy để dọn file ghi dở.
                        File.Delete(path);
                    }
                    catch (Exception cleanupError) when (cleanupError is IOException or UnauthorizedAccessException)
                    {
                        throw new AggregateException("Lưu ảnh thất bại và không dọn được file ghi dở.", saveError, cleanupError);
                    }
                }
                throw;
            }
        }
    }

    public Task DeleteAsync(string fileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || fileName.Contains('\\'))
            throw new ArgumentException("Tên file không hợp lệ.", nameof(fileName));

        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        if (!Guid.TryParseExact(name, "N", out _) || (extension != ".jpg" && extension != ".png"))
            throw new ArgumentException("Tên file không hợp lệ.", nameof(fileName));

        File.Delete(Path.Combine(_storagePath, fileName));
        return Task.CompletedTask;
    }
}
