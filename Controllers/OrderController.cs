using System.Net;
using System.Text.Json;
using EBookStore.Models;
using EBookStore.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBookStore.Controllers
{
    public class OrderController : Controller
    {

        private readonly BookStoreContext _context;

        public OrderController(BookStoreContext context)
        {
            _context = context;
        }


        //GET : My Orders view with Customer Orders
        [HttpGet]
        public async Task<IActionResult> MyOrder()
        {
            var userId = HttpContext.Session.GetString("UserID");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account"); // Redirect to login if not logged in
            }

            // Fetch orders placed by the customer, ordered by status
            var orders = await _context.Orders
                .Where(o => o.UserID == int.Parse(userId))
                .OrderBy(o => o.OrderStatus == "Pending" ? 0 : 1) //Sort Pending orders at the top
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
                .ToListAsync();

            return View(orders);
        }

        // GET : Cart View
        [HttpGet]
        public IActionResult Cart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();
            return View(cart);
        }

        // GET : Manage Order View with Order Data
        [HttpGet]
        public async Task<IActionResult> OrderManagement()
		{
			// Fetch all orders and include necessary navigation properties
			var orders = await _context.Orders
				.Include(o => o.User)
				.Include(o => o.OrderDetails)
				.ThenInclude(od => od.Book)
				.ToListAsync();

			return View(orders); // Passing the list of orders to the view
		}

		// POST : Items Add to Cart Method With View
		[HttpPost]
        public async Task<IActionResult> AddToCart(int bookId, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();

            var cartItem = cart.FirstOrDefault(x => x.BookId == bookId);

            var book = await _context.Books.FindAsync(bookId);

            if (cartItem == null)
            {
                var itemCount = HttpContext.Session.GetString("CartItemCount") ?? "0";
                var newItemCount = int.Parse(itemCount) + 1;
                HttpContext.Session.SetString("CartItemCount", newItemCount.ToString());


                cart.Add(new CartItemViewModel { BookId = bookId, book = book, Quantity = quantity });
            }
            else
            {
                cartItem.Quantity += quantity; // Update quantity if the item already exists
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return RedirectToAction("BookStore", "Books");
        }

        // POST : Items Remove From Cart Method With View
        [HttpPost]
        public IActionResult RemoveFromCart(int bookId)
        {
            // Retrieve the cart from the session
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart");

            if (cart != null)
            {
                // Find the item to remove
                var itemToRemove = cart.FirstOrDefault(c => c.book!.BookID == bookId);

                if (itemToRemove != null)
                {
                    var itemCount = HttpContext.Session.GetString("CartItemCount") ?? "0";
                    var newItemCount = int.Parse(itemCount) - 1;
                    HttpContext.Session.SetString("CartItemCount", newItemCount.ToString());
                    cart.Remove(itemToRemove); // Remove the item from the cart
                }

                // Update the session with the modified cart
                HttpContext.Session.SetObjectAsJson("Cart", cart);
            }

            return RedirectToAction("Cart");
        }

        // POST : Place Order Method
        [HttpPost]
        public async Task<IActionResult> PlaceOrder()
        {
            // Check if user is logged in and has items in the cart
            var userRole = HttpContext.Session.GetString("Role");
            var cartItems = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart");
            var userId = HttpContext.Session.GetString("UserID")!;

            if (userRole == null || cartItems == null || cartItems.Count == 0)
            {
                return BadRequest("User not logged in or cart is empty.");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));

            if (user == null)
            {
                return BadRequest("User not found.");
            }

            // Proceed with placing the order
            var order = new Order
            {
                UserID = int.Parse(userId),
                OrderDate = DateTime.Now,
                TotalAmount = cartItems.Sum(item => item.book!.Price * item.Quantity),
                OrderStatus = "Pending",
                User = user!

            };

            _context.Orders.Add(order);
           

            // Save the order details
            foreach (var item in cartItems)
            {
                var book = await _context.Books.FindAsync(item.BookId);

                if (book == null)
                {
                    return BadRequest($"Book with ID {item.BookId} not found.");
                }

                // Decrease the stock quantity
                if (book.QuantityInStock < item.Quantity)
                {
                    return BadRequest($"Insufficient stock for book: {book.Title}. Available quantity: {book.QuantityInStock}.");
                }

                book.QuantityInStock -= item.Quantity;

                var orderDetail = new OrderDetail
                {
                    
                    Price = item.book!.Price,
                    Quantity = item.Quantity,
                    Order= order,
                    Book = book!
                };

                _context.OrderDetails.Add(orderDetail);
            }

            await _context.SaveChangesAsync();

            // Clear the cart
            HttpContext.Session.Remove("Cart");
            HttpContext.Session.SetString("CartItemCount", "0");

            return Ok(new { message = "Order placed successfully." });
        }

        // POST : Cancel Order Method
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            // Retrieve the order and its details
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null || order.OrderStatus != "Pending")
            {
                return BadRequest("Order not found or cannot be canceled.");
            }

            // Add the quantities back to the stock
            foreach (var orderDetail in order.OrderDetails)
            {
                var book = await _context.Books.FindAsync(orderDetail.BookID);

                if (book != null)
                {
                    // Add back the order quantity to the book's stock
                    book.QuantityInStock += orderDetail.Quantity;
                    _context.Books.Update(book);
                }
            }

            // Mark the order as canceled
            order.OrderStatus = "Canceled";
            _context.Orders.Update(order);

            // Save the changes to the database
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order canceled and stock updated successfully." });
        }

        // POST : Order Again for Cancel Orders Method
        [HttpPost]
        public async Task<IActionResult> OrderAgain(int orderId)
        {
            // Retrieve the existing canceled order and its details
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null || order.OrderStatus != "Canceled")
            {
                return BadRequest(new { message = "Order not found or it is not canceled." });
            }

            // Check stock for each item in the order
            foreach (var orderDetail in order.OrderDetails)
            {
                var book = await _context.Books.FindAsync(orderDetail.BookID);
                if (book == null)
                {
                    return BadRequest(new { message = $"Book with ID {orderDetail.BookID} not found." });
                }

                // Check if enough stock is available for each item
                if (book.QuantityInStock < orderDetail.Quantity)
                {
                    return BadRequest(new { message = $"Insufficient stock for '{book.Title}'. Only {book.QuantityInStock} items left." });
                }
            }

            // Update the order status and decrease stock if all items have enough stock
            foreach (var orderDetail in order.OrderDetails)
            {
                var book = await _context.Books.FindAsync(orderDetail.BookID);

                // Decrease stock and update the book record
                if (book != null && book.QuantityInStock >= orderDetail.Quantity)
                {
                    book.QuantityInStock -= orderDetail.Quantity;
                    _context.Books.Update(book);
                }
            }

            // Update order status to "Pending"
            order.OrderStatus = "Pending";
            order.OrderDate = DateTime.Now; // Update the order date if needed
            _context.Orders.Update(order);

            // Save changes to the database
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order placed again successfully." });
        }

        // POST : Change Order Status Method
		[HttpPost]
		public async Task<IActionResult> ChangeOrderStatus(int orderId, string newStatus)
		{
			var order = await _context.Orders.FindAsync(orderId);
			if (order == null)
			{
				return BadRequest("Order not found.");
			}

			// Update order status
			order.OrderStatus = newStatus;
			await _context.SaveChangesAsync();

			return Ok();
		}

        // GET : Get Order Details By ID
        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int orderId)
		{
			var order = await _context.Orders
		.Include(o => o.User)
		.Include(o => o.OrderDetails)
		.ThenInclude(od => od.Book)
		.FirstOrDefaultAsync(o => o.OrderID == orderId);

			if (order == null)
			{
				return NotFound("Order not found.");
			}

			// Map order to ViewModel
			var orderViewModel = new OrderViewModel
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
					Price = od.Price
				}).ToList() ?? new List<OrderItemViewModel>()
			};

			return Json(orderViewModel);
		}
	}

}
