using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFlow.Api.Dtos.Orders;
using ShopFlow.Api.Exceptions;
using ShopFlow.Api.Services;

namespace ShopFlow.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = "Customer")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var userId) || userId <= 0)
            return Unauthorized();

        try
        {
            var response = await _orderService.CheckoutAsync(userId, request);
            if (response == null) return NotFound();

            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (OrderRuleException ex)
        {
            return Conflict(new { code = ex.Code, message = ex.Message });
        }
        catch (OrderConcurrencyException ex)
        {
            return Conflict(new { code = "CONCURRENCY_CONFLICT", message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var userId) || userId <= 0)
            return Unauthorized();

        var response = await _orderService.GetByIdAsync(id, userId);
        if (response == null) return NotFound();

        return Ok(response);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyOrders(int page = 1, int size = 20, OrderStatus? status = null)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var userId) || userId <= 0)
            return Unauthorized();

        try
        {
            var response = await _orderService.GetListForUserAsync(userId, page, size, status);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{orderId:int}/complete")]
    public async Task<IActionResult> CompleteOrder(int orderId)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var userId) || userId <= 0)
            return Unauthorized();

        try
        {
            var response = await _orderService.CompleteForUserAsync(orderId, userId);
            if (response == null) return NotFound();
            return Ok(response);
        }
        catch (OrderRuleException ex)
        {
            return Conflict(new { code = ex.Code, message = ex.Message });
        }
        catch (OrderConcurrencyException ex)
        {
            return Conflict(new { code = "CONCURRENCY_CONFLICT", message = ex.Message });
        }
    }

    [HttpPost("{orderId:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var userId) || userId <= 0)
            return Unauthorized();

        try
        {
            var response = await _orderService.CancelForUserAsync(orderId, userId);
            if (response == null) return NotFound();
            return Ok(response);
        }
        catch (OrderRuleException ex)
        {
            return Conflict(new { code = ex.Code, message = ex.Message });
        }
        catch (OrderConcurrencyException ex)
        {
            return Conflict(new { code = "CONCURRENCY_CONFLICT", message = ex.Message });
        }
    }


}
