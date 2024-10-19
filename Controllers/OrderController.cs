using System.Net;
using EBookStore.Models;
using EBookStore.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EBookStore.Controllers
{
    public class OrderController : Controller
    {

        private readonly BookStoreContext _context;

        public OrderController(BookStoreContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET : Cart View
        public IActionResult Cart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart") ?? new List<CartItemViewModel>();
            return View(cart);
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
    }

}
