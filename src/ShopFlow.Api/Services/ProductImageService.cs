

using ShopFlow.Api.Dtos.Products;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Repositories;

namespace ShopFlow.Api.Services;

public class ProductImageService
{
    private readonly IProductImageStorage _productImageStorage;
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _productImageRepository;

    public ProductImageService(IProductImageRepository productImageRepository, IProductRepository productRepository, IProductImageStorage productImageStorage)
    {
        _productImageRepository = productImageRepository;
        _productRepository = productRepository;
        _productImageStorage = productImageStorage;
    }

    public async Task<ProductImageResponse?> UploadAsync(int productId, IFormFile file, string? altText, int sortOrder, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string description = altText?.Trim() ?? string.Empty;
        if (description.Length > 200)
            throw new ArgumentException("Mô tả ảnh không được quá 200 ký tự.", nameof(altText));
        if (sortOrder < 0)
            throw new ArgumentException("Thứ tự ảnh không được nhỏ hơn 0.", nameof(sortOrder));

        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null) return null;
        var fileName = await _productImageStorage.SaveAsync(file, cancellationToken);

        var image = new ProductImage
        {
            ProductId = productId,
            FileName = fileName,
            AltText = description,
            SortOrder = sortOrder
        };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _productImageRepository.Add(image);
            await _productImageRepository.SaveChangeAsync();
        }
        catch (Exception saveError)
        {
            try
            {
                // Request đã hủy vẫn cần dọn file vừa lưu nếu ghi DB thất bại.
                await _productImageStorage.DeleteAsync(fileName, CancellationToken.None);
            }
            catch (Exception cleanupError)
            {
                throw new AggregateException("Lưu thông tin ảnh thất bại và không dọn được file.", saveError, cleanupError);
            }
            throw;
        }

        return new ProductImageResponse
        {
            Id = image.Id,
            ProductId = image.ProductId,
            FileName = image.FileName,
            AltText = image.AltText,
            SortOrder = image.SortOrder
        };
    }
}
