using EBookStore.Models;
using EBookStore.Models.DTO;
using EBookStore.Models.Entity;
using EBookStore.Util;
using Microsoft.EntityFrameworkCore;
using PdfWriter = iText.Kernel.Pdf.PdfWriter;

namespace EBookStore.services.Impl
{
    public class OrderService : IOrderService
    {
        private readonly BookStoreContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderService(BookStoreContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<Order>> GetCustomerOrdersAsync(int userId)
        {
            return await _context.Orders
                .Where(o => o.Id == userId)
                .OrderBy(o => o.OrderStatus == "Pending" ? 0 : 1) // Sort Pending orders at the top
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
                .ToListAsync();
        }

        public List<CartItemViewModel> GetCartItems()
        {
            var cart = _httpContextAccessor.HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart")
                       ?? new List<CartItemViewModel>();
            return cart;
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
                .ToListAsync();
        }

        public async Task AddToCartAsync(int bookId, int quantity)
        {
            var cart = _httpContextAccessor.HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();
            var cartItem = cart.FirstOrDefault(x => x.BookId == bookId);

            var book = await _context.Books.FindAsync(bookId);

            if (cartItem == null)
            {
                var itemCount = _httpContextAccessor.HttpContext.Session.GetString("CartItemCount") ?? "0";
                var newItemCount = int.Parse(itemCount) + 1;
                _httpContextAccessor.HttpContext.Session.SetString("CartItemCount", newItemCount.ToString());

                cart.Add(new CartItemViewModel { BookId = bookId, book = book, Quantity = quantity });
            }
            else
            {
                cartItem.Quantity += quantity;
            }

            _httpContextAccessor.HttpContext.Session.SetObjectAsJson("Cart", cart);
        }

        public void UpdateCartItemQuantity(int bookId, int quantity)
        {
            var cart = _httpContextAccessor.HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();
            var cartItem = cart.FirstOrDefault(x => x.BookId == bookId);

            if (cartItem != null)
            {
                cartItem.Quantity = quantity;
                _httpContextAccessor.HttpContext.Session.SetObjectAsJson("Cart", cart);
            }
        }

        public void RemoveCartItem(int bookId)
        {
            var cart = _httpContextAccessor.HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart");
            if (cart != null)
            {
                var itemToRemove = cart.FirstOrDefault(c => c.book!.BookID == bookId);
                if (itemToRemove != null)
                {
                    var itemCount = _httpContextAccessor.HttpContext.Session.GetString("CartItemCount") ?? "0";
                    var newItemCount = int.Parse(itemCount) - 1;
                    _httpContextAccessor.HttpContext.Session.SetString("CartItemCount", newItemCount.ToString());

                    cart.Remove(itemToRemove);
                    _httpContextAccessor.HttpContext.Session.SetObjectAsJson("Cart", cart);
                }
            }
        }

        public async Task<(bool success, string message)> PlaceOrderAsync(string userId, List<CartItemViewModel> cartItems)
        {
            // Check if user exists
            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return (false, "User not found.");
            }

            // Create the order
            var order = new Order
            {
                Id = int.Parse(userId),
                OrderDate = DateTime.Now,
                TotalAmount = cartItems.Sum(item => item.book!.Price * item.Quantity),
                OrderStatus = "Pending",
                User = user
            };

            _context.Orders.Add(order);

            // Add order details and adjust stock
            foreach (var item in cartItems)
            {
                var book = await _context.Books.FindAsync(item.BookId);
                if (book == null)
                {
                    return (false, $"Book with ID {item.BookId} not found.");
                }

                if (book.QuantityInStock < item.Quantity)
                {
                    return (false, $"Insufficient stock for book: {book.Title}. Available quantity: {book.QuantityInStock}.");
                }

                book.QuantityInStock -= item.Quantity;

                var orderDetail = new OrderDetail
                {
                    Price = item.book!.Price,
                    Quantity = item.Quantity,
                    Order = order,
                    Book = book
                };

                _context.OrderDetails.Add(orderDetail);
            }

            await _context.SaveChangesAsync();

            // Clear the cart session
            _httpContextAccessor.HttpContext.Session.Remove("Cart");
            _httpContextAccessor.HttpContext.Session.SetString("CartItemCount", "0");

            return (true, "Order placed successfully.");
        }

        public async Task<(bool success, string message)> CancelOrderAsync(int orderId)
        {
            // Retrieve the order and include order details with associated books
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null || order.OrderStatus != "Pending")
            {
                return (false, "Order not found or cannot be canceled.");
            }

            // Update stock quantities
            foreach (var orderDetail in order.OrderDetails)
            {
                var book = await _context.Books.FindAsync(orderDetail.BookID);
                if (book != null)
                {
                    book.QuantityInStock += orderDetail.Quantity;
                    _context.Books.Update(book);
                }
            }

            // Mark the order as canceled
            order.OrderStatus = "Canceled";
            _context.Orders.Update(order);

            await _context.SaveChangesAsync();

            return (true, "Order canceled and stock updated successfully.");
        }

        public async Task<(bool success, string message)> OrderAgainAsync(int orderId)
        {
            // Retrieve the canceled order with order details
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null || order.OrderStatus != "Canceled")
            {
                return (false, "Order not found or it is not canceled.");
            }

            // Verify stock availability for each item
            foreach (var orderDetail in order.OrderDetails)
            {
                var book = await _context.Books.FindAsync(orderDetail.BookID);
                if (book == null || book.QuantityInStock < orderDetail.Quantity)
                {
                    var bookTitle = book?.Title ?? "Unknown";
                    return (false, $"Insufficient stock for '{bookTitle}'. Only {book?.QuantityInStock} items left.");
                }
            }

            // Update stock and order status
            foreach (var orderDetail in order.OrderDetails)
            {
                var book = await _context.Books.FindAsync(orderDetail.BookID);
                if (book != null)
                {
                    book.QuantityInStock -= orderDetail.Quantity;
                    _context.Books.Update(book);
                }
            }

            order.OrderStatus = "Pending";
            order.OrderDate = DateTime.Now; // Update the order date if needed
            _context.Orders.Update(order);

            await _context.SaveChangesAsync();

            return (true, "Order placed again successfully.");
        }

        public async Task<(bool success, string message)> ChangeOrderStatusAsync(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return (false, "Order not found.");
            }

            // Update the order status
            order.OrderStatus = newStatus;
            await _context.SaveChangesAsync();

            return (true, "Order status updated successfully.");
        }

        public async Task<OrderViewModel?> GetOrderDetailsByIdAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
            {
                return null;
            }

            // Map order to ViewModel
            return new OrderViewModel
            {
                OrderID = order.OrderID,
                OrderStatus = order.OrderStatus,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                CustomerName = $"{order.User.FirstName} {order.User.LastName}",
                CustomerEmail = order.User.Email,
                CustomerAddress = order.User.Address,
                OrderItems = order.OrderDetails.Select(od => new OrderItemViewModel
                {
                    BookTitle = od.Book.Title,
                    Quantity = od.Quantity,
                    Price = od.Price,
                    ImageData = $"data:{od.Book.ImageMimeType};base64,{Convert.ToBase64String(od.Book.ImageData)}",
                }).ToList()
            };
        }

        public async Task<byte[]> GenerateOrderPdfAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
            {
                throw new Exception("Order not found.");
            }

            using (var stream = new MemoryStream())
            {
                var writer = new PdfWriter(stream);
                var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
                var document = new iText.Layout.Document(pdf);

                // Add title and customer details
                document.Add(new iText.Layout.Element.Paragraph($"Order Report - Order ID: {orderId}").SetFontSize(18).SetBold());
                document.Add(new iText.Layout.Element.Paragraph($"Customer: {order.User.FirstName} {order.User.LastName}"));
                document.Add(new iText.Layout.Element.Paragraph($"Email: {order.User.Email}"));
                document.Add(new iText.Layout.Element.Paragraph($"Address: {order.User.Address}"));
                document.Add(new iText.Layout.Element.Paragraph($"Order Date: {order.OrderDate:yyyy-MM-dd}"));
                document.Add(new iText.Layout.Element.Paragraph($"Total Amount: {order.TotalAmount.ToString("C", new System.Globalization.CultureInfo("en-LK"))}"));

                // Add order items
                document.Add(new iText.Layout.Element.Paragraph("Order Items:"));
                var table = new iText.Layout.Element.Table(3);
                table.AddHeaderCell("Book Title");
                table.AddHeaderCell("Quantity");
                table.AddHeaderCell("Price");

                foreach (var item in order.OrderDetails)
                {
                    table.AddCell(item.Book.Title);
                    table.AddCell(item.Quantity.ToString());
                    table.AddCell(item.Price.ToString("C", new System.Globalization.CultureInfo("en-LK")));
                }

                document.Add(table);
                document.Close();

                return stream.ToArray();
            }
        }
    }
}
