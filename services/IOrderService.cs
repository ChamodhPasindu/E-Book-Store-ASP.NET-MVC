using EBookStore.Models.DTO;
using EBookStore.Models.Entity;

namespace EBookStore.services
{
    public interface IOrderService
    {
        Task<List<Order>> GetCustomerOrdersAsync(int userId);

        List<CartItemViewModel> GetCartItems();

        Task<List<Order>> GetAllOrdersAsync();

        Task AddToCartAsync(int bookId, int quantity);

        void UpdateCartItemQuantity(int bookId, int quantity);

        void RemoveCartItem(int bookId);

        Task<(bool success, string message)> PlaceOrderAsync(string userId, List<CartItemViewModel> cartItems);

        Task<(bool success, string message)> CancelOrderAsync(int orderId);

        Task<(bool success, string message)> OrderAgainAsync(int orderId);

        Task<(bool success, string message)> ChangeOrderStatusAsync(int orderId, string newStatus);

        Task<OrderViewModel?> GetOrderDetailsByIdAsync(int orderId);

        Task<byte[]> GenerateOrderPdfAsync(int orderId);
    }
}
