using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFlow.Api.Services;
using ShopFlow.Api.Dtos.Carts;
using ShopFlow.Api.Exceptions;

namespace ShopFlow.Api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize(Roles = "Customer")]
public class CartController : ControllerBase
{
    private readonly CartService _cartService;

    public CartController(CartService cartService)
    {
        _cartService = cartService;
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
    {
        var subject = User.FindFirst("sub")?.Value;
        if (!int.TryParse(subject, out var userId) || userId <= 0)
            return Unauthorized();

        try
        {
            var response = await _cartService.AddItemAsync(userId, request);
            if (response == null) return NotFound();

            return Ok(response);
        }
        catch (CartConcurrencyException ex)
        {
            return Conflict(new { code = "CONCURRENCY_CONFLICT", message = ex.Message });
        }
        catch (CartRuleException ex)
        {
            return Conflict(new { code = "CART_RULE_VIOLATION", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("items/{productId:int}")]
    public async Task<IActionResult> UpdateItem(int productId, [FromBody] UpdateCartItemRequest request)
    {
        var subject = User.FindFirst("sub")?.Value;
        if (!int.TryParse(subject, out var userId) || userId <= 0)
            return Unauthorized();

        try
        {
            var response = await _cartService.UpdateAsync(productId, userId, request);
            if (response == null) return NotFound();

            return Ok(response);
        }
        catch (CartConcurrencyException ex)
        {
            return Conflict(new { code = "CONCURRENCY_CONFLICT", message = ex.Message });
        }
        catch (CartRuleException ex)
        {
            return Conflict(new { code = "CART_RULE_VIOLATION", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("items/{productId:int}")]
    public async Task<IActionResult> DeleteItem(int productId)
    {
        var subject = User.FindFirst("sub")?.Value;
        if (!int.TryParse(subject, out var userId) || userId <= 0)
            return Unauthorized();

        try
        {
            var response = await _cartService.DeleteAsync(productId, userId);
            if (response == null) return NotFound();

            return Ok(response);
        }
        catch (CartConcurrencyException ex)
        {
            return Conflict(new { code = "CONCURRENCY_CONFLICT", message = ex.Message });
        }
    }

    [HttpGet("items")]
    public async Task<IActionResult> GetItems()
    {
        var subject = User.FindFirst("sub")?.Value;
        if (!int.TryParse(subject, out var userId) || userId <= 0)
            return Unauthorized();

        var response = await _cartService.GetCartAsync(userId);
        if (response == null) return NotFound();

        return Ok(response);
    }
}
