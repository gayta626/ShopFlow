

using ShopFlow.Api.Dtos.Carts;
using ShopFlow.Api.Repositories;
using ShopFlow.Api.Exceptions;
using ShopFlow.Api.Entities;
using Microsoft.VisualBasic;
namespace ShopFlow.Api.Services;

public class CartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;

    public CartService(ICartRepository cartRepository, IProductRepository productRepository)
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
    }

    public async Task<CartResponse?> GetCartAsync(int userId)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId);

        if (cart == null) return null;
        var items = cart.Items.Select(item => new CartItemResponse
        {
            ProductId = item.ProductID,
            Sku = item.Product.Sku,
            ProductName = item.Product.Name,
            Quantity = item.Quantity,
            UnitPrice = item.Product.Price,
            Stock = item.Product.Stock,
            IsAvailable = item.Product.IsActive
        && item.Product.Stock >= item.Quantity,
            LineTotal = item.Product.Price * item.Quantity
        }).ToList();

        return new CartResponse
        {
            Items = items,
            TotalAmount = items.Sum(item => item.LineTotal)
        };

    }

    public async Task<CartResponse?> AddItemAsync(int userId, AddCartItemRequest request)
    {
        if (request.ProductId <= 0 || request.Quantity < 1 || request.Quantity > 99)
            throw new ArgumentException("ProductId phải lớn hơn 0; Quantity phải từ 1 đến 99.");

        var cart = await _cartRepository.GetByUserIdForUpdateAsync(userId);
        if (cart == null) return null;

        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null) return null;

        if (product.IsActive == false) throw new CartRuleException("San pham da ngung ban");
        var existingItem = cart.Items.FirstOrDefault(item => item.ProductID == product.Id);
        int newQuantity = existingItem == null ? request.Quantity : checked(request.Quantity + existingItem.Quantity);

        if (newQuantity > 99) throw new CartRuleException("Moi san phan chi duoc them toi da 99 chiec");

        if (newQuantity > product.Stock)
            throw new CartRuleException("Sản phẩm không đủ tồn kho.");

        if (existingItem == null)
        {
            if (cart.Items.Count >= 50)
                throw new CartRuleException("Giỏ hàng tối đa 50 sản phẩm khác nhau.");

            cart.Items.Add(new CartItem
            {
                ProductID = product.Id,
                Quantity = newQuantity
            });
        }
        else
        {
            existingItem.Quantity = newQuantity;
        }

        // Lưu Items và Revision cùng một lần để rollback toàn bộ nếu xung đột.
        cart.Revision = checked(cart.Revision + 1);
        await _cartRepository.SaveChangeAsync();
        return await GetCartAsync(userId);
    }
    public async Task<CartResponse?> UpdateAsync(int productId, int userId, UpdateCartItemRequest request)
    {
        if (request.Quantity < 1 || request.Quantity > 99)
        {
            throw new ArgumentException("Quantity phai tu 1 den 99");
        }

        var cart = await _cartRepository.GetByUserIdForUpdateAsync(userId);
        if (cart == null)
        {
            return null;
        }
        var item = cart.Items.FirstOrDefault(c => c.ProductID == productId);
        if (item == null) return null;

        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null) return null;

        if (product.IsActive == false)
        {
            throw new CartRuleException("San pham da ngung ban");
        }

        if (product.Stock < request.Quantity)
        {
            throw new CartRuleException("san pham khong du ton kho");
        }

        item.Quantity = request.Quantity;

        cart.Revision = checked(cart.Revision + 1);
        await _cartRepository.SaveChangeAsync();
        return await GetCartAsync(userId);

    }

    public async Task<CartResponse?> DeleteAsync(int productId, int userId)
    {
        var cart = await _cartRepository.GetByUserIdForUpdateAsync(userId);
        if (cart == null) return null;

        var item = cart.Items.FirstOrDefault(c => c.ProductID == productId);
        if (item == null) return null;

        _cartRepository.RemoveItem(item);

        cart.Revision = checked(cart.Revision + 1);
        await _cartRepository.SaveChangeAsync();
        return await GetCartAsync(userId);
    }
}
