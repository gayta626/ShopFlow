using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopFlow.Api.Dtos.Orders;
using ShopFlow.Api.Exceptions;
using ShopFlow.Api.Services;

namespace ShopFlow.Api.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public AdminOrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPut("{orderId:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int orderId, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            var response = await _orderService.UpdateStatusForAdminAsync(orderId, request);
            if (response == null) return NotFound();

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var response = await _orderService.GetByIdForAdminAsync(id);
        if (response == null) return NotFound();

        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders(int page = 1, int size = 20, OrderStatus? status = null)
    {
        try
        {
            var response = await _orderService.GetListForAdminAsync(page, size, status);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
