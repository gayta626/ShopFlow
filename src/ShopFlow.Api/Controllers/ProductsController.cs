using Microsoft.AspNetCore.Mvc;
using ShopFlow.Api.Dtos.Products;
using ShopFlow.Api.Services;

namespace ShopFlow.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductsService _productsService;

    public ProductsController(ProductsService productsService)
    {
        _productsService = productsService;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _productsService.GetPublicById(id);
        if (response == null) return NotFound();

        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetListPublic([FromQuery] ProductQueryRequest request)
    {
        try
        {
            var response = await _productsService.GetPublicListAsync(request);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
