using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopFlow.Api.Dtos.Categories;
using ShopFlow.Api.Exceptions;
using ShopFlow.Api.Services;

namespace ShopFlow.Api.Controllers;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")] // Mọi action trong controller này đều yêu cầu quyền Admin.
public class AdminCategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;

    public AdminCategoriesController(CategoryService categoryService)
    {
        _categoryService = categoryService; // DI cung cấp service đã đăng ký trong Program.cs.
    }

    [HttpGet] // GET /api/admin/categories
    public async Task<IActionResult> GetAll()
    {
        var categories = await _categoryService.GetAllAsync();
        return Ok(categories); // Trả 200 cùng danh sách DTO.
    }

    [HttpGet("{id:int}")] // GET /api/admin/categories/5
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category == null)
            return NotFound(); // ID không tồn tại thì trả 404.

        return Ok(category);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
    {
        try
        {
            var response = await _categoryService.UpdateAsync(id, request);
            if (response == null) return NotFound();
            return Ok(response);
        }
        catch (CategoryRenameConfirmationRequiredException ex)
        {
            // Giao diện nhận mã này để hỏi Admin, rồi gửi lại confirmRename=true.
            return Conflict(new { code = "CATEGORY_RENAME_CONFIRMATION_REQUIRED", message = ex.Message });
        }
        catch (CategoryAlreadyExistsException ex)
        {
            return Conflict(new { code = "CATEGORY_EXISTS", message = ex.Message });
        }
        catch (CategoryConcurrencyException ex)
        {
            return Conflict(new { code = "CONCURRENCY_CONFLICT", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost] // POST /api/admin/categories
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        try
        {
            var response = await _categoryService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response); // 201 và Location trỏ đến action chi tiết.
        }
        catch (CategoryAlreadyExistsException ex)
        {
            return Conflict(new { message = ex.Message }); // Trùng tên danh mục: 409.
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message }); // Tên không hợp lệ sau trim: 400.
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id)
    {
        try
        {
            bool deleted = await _categoryService.DeleteAsync(id);
            if (!deleted) return NotFound();

            return NoContent();
        }
        catch (CategoryInUseException ex)
        {
            return Conflict(new
            {
                code = "CATEGORY_IN_USE",
                message = ex.Message
            });
        }

    }
}
