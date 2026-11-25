
using ShopFlow.Api.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFlow.Api.Dtos.Products;
using ShopFlow.Api.Products;
using ShopFlow.Api.Entities;

using ShopFlow.Api.Services;

namespace ShopFlow.Api.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = "Admin")]
public class AdminProductsController : ControllerBase
{
    private readonly ProductsService _productsService;
    private readonly ProductImageService _productImageService;

    public AdminProductsController(ProductsService productsService, ProductImageService productImageService)
    {
        _productsService = productsService;
        _productImageService = productImageService;
    }
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        try
        {
            var response = await _productsService.CreateAsync(request);
            return CreatedAtAction(
                    nameof(GetById),
                    new { id = response.Id },
                    response);
        }
        catch (ProductAlreadyExistException ex)
        {
            return Conflict(new
            {
                code = "SKU_EXISTS",
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CategoryNotFoundException ex)
        {
            return BadRequest(new
            {
                code = "CATEGORY_NOT_FOUND",
                message = ex.Message
            });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
    {
        try
        {
            var response = await _productsService.UpdateAsync(id, request);
            if (response == null) return NotFound();

            return Ok(response);
        }
        catch (ProductConcurrencyException ex)
        {
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message
            });
        }
        catch (ProductAlreadyExistException ex)
        {
            return Conflict(new
            {
                code = "SKU_EXISTS",
                message = ex.Message
            });
        }
        catch (CategoryNotFoundException ex)
        {
            return BadRequest(new
            {
                code = "CATEGORY_NOT_FOUND",
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ProductConfigurationLockedException ex)
        {
            return Conflict(new
            {
                code = "PRODUCT_CONFIGURATION_LOCKED",
                message = ex.Message
            });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _productsService.GetByIdAsync(id);
        if (response == null) return NotFound();

        return Ok(response);
    }
    [HttpGet]
    public async Task<IActionResult> GetList(
    [FromQuery] int page = 1,
    [FromQuery] int size = 20)
    {
        try
        {
            var response = await _productsService.GetListAsync(page, size);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            // Service ngừng bán sản phẩm bằng IsActive, không xóa bản ghi.
            var success = await _productsService.DeactivateAsync(id);
            if (!success) return NotFound();

            // Đã ngừng bán từ trước cũng trả 204, không có response body.
            return NoContent();
        }
        catch (ProductConcurrencyException ex)
        {
            // Sản phẩm bị cập nhật đồng thời giữa lúc service đọc và lưu.
            return Conflict(new
            {
                code = "CONCURRENCY_CONFLICT",
                message = ex.Message
            });
        }
    }

    [HttpDelete("{productId:int}/images/{imageId:int}")]
    public async Task<IActionResult> DeleteAsync(int productId, int imageId, CancellationToken cancellationToken)
    {
        var deleted = await _productImageService.DeleteAsync(productId, imageId, cancellationToken);
        if (!deleted) return NotFound();

        return NoContent();
    }

    [HttpPost("{productId:int}/images")]

    public async Task<IActionResult> UploadAsync([FromRoute] int productId, [FromForm] IFormFile file, CancellationToken cancellationToken, [FromForm] string? altText = null, [FromForm] int sortOrder = 0)
    {
        try
        {
            var image = await _productImageService.UploadAsync(
                productId, file, altText, sortOrder, cancellationToken);

            if (image == null) return NotFound();

            return StatusCode(StatusCodes.Status201Created, image);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

}
