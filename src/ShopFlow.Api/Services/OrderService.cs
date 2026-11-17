using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ShopFlow.Api.Dtos.Orders;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Exceptions;
using ShopFlow.Api.Repositories;

namespace ShopFlow.Api.Services;

public class OrderService
{
    private readonly IOrderRepository _orderRepository;

    public OrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderResponse?> CheckoutAsync(int userId, CheckoutRequest request)
    {
        var address = request.ShippingAddress?.Trim() ?? string.Empty;
        if (address.Length < 10 || address.Length > 500)
            throw new ArgumentException("Địa chỉ giao hàng phải có từ 10 đến 500 ký tự.");

        try
        {
            // Thoát phạm vi mà chưa commit sẽ rollback khi transaction được dispose.
            await using var transaction = await _orderRepository.BeginTransactionAsync();
            var cart = await _orderRepository.GetCartForCheckoutAsync(userId);
            if (cart == null) return null;
            if (cart.Items.Count == 0)
                throw new ArgumentException("Giỏ hàng đang trống.");

            foreach (var item in cart.Items)
            {
                if (!item.Product.IsActive)
                    throw new OrderRuleException("PRODUCT_UNAVAILABLE", $"Sản phẩm {item.Product.Sku} đã ngừng bán.");
                if (item.Product.Stock < item.Quantity)
                    throw new OrderRuleException("INSUFFICIENT_STOCK", $"Sản phẩm {item.Product.Sku} không đủ tồn kho.");
            }

            var order = new Order
            {
                UserId = userId,
                ShippingAddress = address,
                Status = OrderStatus.Pending
            };

            foreach (var item in cart.Items)
            {
                // Lưu bản chụp để thay đổi catalog sau này không đổi lịch sử đơn.
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductID,
                    Sku = item.Product.Sku,
                    ProductName = item.Product.Name,
                    UnitPrice = item.Product.Price,
                    Quantity = item.Quantity
                });
                item.Product.Stock -= item.Quantity;
            }

            order.TotalAmount = order.Items.Sum(item => item.UnitPrice * item.Quantity);
            cart.Revision = checked(cart.Revision + 1);
            _orderRepository.Add(order);
            _orderRepository.RemoveCartItems(cart.Items);
            await _orderRepository.SaveChangeAsync();
            await transaction.CommitAsync();

            return new OrderResponse
            {
                Id = order.Id,
                Status = order.Status.ToString(),
                ShippingAddress = order.ShippingAddress,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                Items = order.Items.Select(item => new OrderItemResponse
                {
                    ProductId = item.ProductId,
                    Sku = item.Sku,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    LineTotal = item.UnitPrice * item.Quantity
                }).ToList()
            };
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            // Bắt cả lỗi khi đọc, lưu hoặc commit; kết thúc request, không tự retry.
            throw new OrderConcurrencyException(ex);
        }
    }

    public async Task<OrderResponse?> GetByIdAsync(int id, int userId)
    {
        var order = await _orderRepository.GetByIdForUserAsync(id, userId);
        if (order == null) return null;

        return new OrderResponse
        {
            Id = order.Id,
            Status = order.Status.ToString(),
            ShippingAddress = order.ShippingAddress,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            Items = order.Items.OrderBy(item => item.Id).Select(item => new OrderItemResponse
            {
                ProductId = item.ProductId,
                Sku = item.Sku,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                LineTotal = item.UnitPrice * item.Quantity
            }).ToList()
        };
    }

    private static bool IsConcurrencyConflict(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is DbUpdateConcurrencyException)
                return true;
            if (current is PostgresException postgres &&
                postgres.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
                return true;
        }
        return false;
    }

    public async Task<OrderListResponse> GetListForUserAsync(
    int userId, int page, int size, OrderStatus? status)
    {
        if (page < 1 || size < 1 || size > 100)
        {
            throw new ArgumentException("Trang khong duoc nho hon 1 va so luong dong trong 1 trang phai nam tu 1 den 100");
        }

        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            throw new ArgumentException("Trang thai don hang khong dung dinh dang");
        }

        if ((long)(page - 1) * size > int.MaxValue)
        {
            throw new ArgumentException("Trang yeu cau vuot qua gioi han cho phep");
        }


        var totalCount = await _orderRepository.CountForUserAsync(userId, status);
        var orders = await _orderRepository.GetPageForUserAsync(userId, page, size, status);

        return new OrderListResponse
        {
            Page = page,
            Size = size,
            TotalCount = totalCount,
            Items = orders.Select(order => new OrderSummaryResponse
            {
                Id = order.Id,
                Status = order.Status.ToString(),
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt
            }).ToList()
        };
    }

    public async Task<OrderResponse?> CancelForUserAsync(int orderId, int userId)
    {
        try
        {
            await using var transaction = await _orderRepository.BeginTransactionAsync();
            var order = await _orderRepository.GetOrderForUpdateAsync(orderId, userId);

            if (order == null) return null;
            if (order.Status != OrderStatus.Cancelled)
            {
                if (order.Status != OrderStatus.Pending)
                {
                    throw new OrderRuleException(
                    "INVALID_ORDER_STATUS",
                    "Chi huy duoc don dang cho xu ly"
                );
                }

                order.Status = OrderStatus.Cancelled;
                order.CancelledAt = DateTimeOffset.UtcNow;

                foreach (var item in order.Items)
                {
                    item.Product.Stock = checked(item.Product.Stock + item.Quantity);
                }
                await _orderRepository.SaveChangeAsync();
            }

            await transaction.CommitAsync();

            return new OrderResponse
            {
                Id = order.Id,
                Status = order.Status.ToString(),
                ShippingAddress = order.ShippingAddress,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                Items = order.Items.OrderBy(item => item.Id)
                .Select(item => new OrderItemResponse
                {
                    ProductId = item.ProductId,
                    Sku = item.Sku,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    LineTotal = item.UnitPrice * item.Quantity
                }).ToList()
            };
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            throw new OrderConcurrencyException(ex);
        }
    }

    public async Task<OrderResponse?> UpdateStatusForAdminAsync(int orderId, UpdateOrderStatusRequest request)
    {
        if (!request.Status.HasValue || !Enum.IsDefined(request.Status.Value))
            throw new ArgumentException("Trạng thái đơn hàng không hợp lệ.");

        var newStatus = request.Status.Value;
        try
        {
            await using var transaction = await _orderRepository.BeginTransactionAsync();
            var order = await _orderRepository.GetOrderForAdminUpdateAsync(orderId);
            if (order == null) return null;

            // Gửi lại trạng thái hiện tại không đổi timestamp hoặc hoàn kho thêm.
            if (order.Status != newStatus)
            {
                var canTransition =
                    (order.Status == OrderStatus.Pending &&
                        (newStatus == OrderStatus.Paid || newStatus == OrderStatus.Cancelled)) ||
                    (order.Status == OrderStatus.Paid &&
                        (newStatus == OrderStatus.Shipped || newStatus == OrderStatus.Cancelled));

                if (!canTransition)
                    throw new OrderRuleException(
                        "INVALID_ORDER_STATUS",
                        "Admin không được chuyển đơn sang trạng thái này.");

                order.Status = newStatus;
                var now = DateTimeOffset.UtcNow;
                switch (newStatus)
                {
                    case OrderStatus.Paid:
                        order.PaidAt = now;
                        break;
                    case OrderStatus.Shipped:
                        order.ShippedAt = now;
                        break;
                    case OrderStatus.Cancelled:
                        order.CancelledAt = now;
                        foreach (var item in order.Items)
                        {
                            item.Product.Stock = checked(item.Product.Stock + item.Quantity);
                        }
                        break;
                }

                await _orderRepository.SaveChangeAsync();
            }

            await transaction.CommitAsync();
            return new OrderResponse
            {
                Id = order.Id,
                Status = order.Status.ToString(),
                ShippingAddress = order.ShippingAddress,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                Items = order.Items.OrderBy(item => item.Id)
                    .Select(item => new OrderItemResponse
                    {
                        ProductId = item.ProductId,
                        Sku = item.Sku,
                        ProductName = item.ProductName,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        LineTotal = item.UnitPrice * item.Quantity
                    }).ToList()
            };
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            throw new OrderConcurrencyException(ex);
        }
    }

    public async Task<OrderResponse?> CompleteForUserAsync(int orderId, int userId)
    {
        try
        {
            await using var transaction = await _orderRepository.BeginTransactionAsync();

            var order = await _orderRepository.GetOrderForUpdateAsync(orderId, userId);
            if (order == null) return null;

            if (order.Status != OrderStatus.Completed)
            {
                if (order.Status != OrderStatus.Shipped)
                {
                    throw new OrderRuleException("INVALID_ORDER_STATUS",
                    "Don chua chuyen sang trang thai giao hang");
                }
                order.Status = OrderStatus.Completed;
                order.CompletedAt = DateTimeOffset.UtcNow;

                await _orderRepository.SaveChangeAsync();
            }

            await transaction.CommitAsync();

            return new OrderResponse
            {
                Id = order.Id,
                Status = order.Status.ToString(),
                ShippingAddress = order.ShippingAddress,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                Items = order.Items.OrderBy(item => item.Id)
                    .Select(item => new OrderItemResponse
                    {
                        ProductId = item.ProductId,
                        Sku = item.Sku,
                        ProductName = item.ProductName,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        LineTotal = item.UnitPrice * item.Quantity
                    }).ToList()
            };
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            throw new OrderConcurrencyException(ex);
        }
    }
}
