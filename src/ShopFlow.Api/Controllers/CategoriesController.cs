

using Microsoft.AspNetCore.Mvc;
using ShopFlow.Api.Services;

namespace ShopFlow.Api.Controllers;

[ApiController] // Áp dụng hành vi API cho cả controller.
[Route("api/categories")] // Địa chỉ chung của controller công khai.
public class CategoriesController : ControllerBase // Cung cấp các hàm trả HTTP như Ok().
{

    private readonly CategoryService _categoryService;

    public CategoriesController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _categoryService.GetAllAsync();
        return Ok(categories); // Service đã trả danh sách DTO gồm Id/Name, không cần map lại.
    }
}
