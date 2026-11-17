

using System.ComponentModel.DataAnnotations;

namespace ShopFlow.Api.Dtos.Orders;

public class UpdateOrderStatusRequest
{
    [Required]
    public OrderStatus? Status { get; set; }

}