using EBookStore.Models.DTO;
using EBookStore.services;
using EBookStore.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBookStore.Controllers
{
    public class OrderController : Controller
	{
		private readonly IOrderService _orderService;

		public OrderController(IOrderService orderService)
		{
			_orderService = orderService;
		}

        //GET : My Orders view with Customer Orders
        [HttpGet]
		public async Task<IActionResult> MyOrder()
		{
            try
            {
                var userId = HttpContext.Session.GetString("UserID");
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account"); // Redirect to login if not logged in
                }

                var orders = await _orderService.GetCustomerOrdersAsync(int.Parse(userId));
                return View(orders);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while retrieving order list. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

		// GET : Cart View
		[HttpGet]
		public IActionResult Cart()
		{
            try
            {
                var cart = _orderService.GetCartItems();
                return View(cart);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while retrieving cart item list. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

        // GET : Manage Order View with Order Data
        [HttpGet]
		public async Task<IActionResult> OrderManagement()
		{
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                return View(orders);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while retrieving order list. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

		// POST : Items Add to Cart Method With View
		[HttpPost]
		public async Task<IActionResult> AddToCart(int bookId, int quantity)
		{
            try
            {
                await _orderService.AddToCartAsync(bookId, quantity);
                return RedirectToAction("BookStore", "Books");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while adding items to the cart. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

		// POST : Update Items in Cart Method With View
		[HttpPost]
		public IActionResult UpdateQuantity(int bookId, int quantity)
		{
            try
            {
                _orderService.UpdateCartItemQuantity(bookId, quantity);
                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating item quantity. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

		// POST : Items Remove From Cart Method With View
		[HttpPost]
		public IActionResult RemoveFromCart(int bookId)
		{
            try
            {
                _orderService.RemoveCartItem(bookId);
                return RedirectToAction("Cart");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while removing item from the cart. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

        // POST : Place Order Method
        [HttpPost]
		public async Task<IActionResult> PlaceOrder()
		{
            try
            {
                var userRole = HttpContext.Session.GetString("Role");
                var cartItems = HttpContext.Session.GetObjectFromJson<List<CartItemViewModel>>("Cart");
                var userId = HttpContext.Session.GetString("UserID");

                if (userRole == null || cartItems == null || cartItems.Count == 0)
                {
                    return BadRequest("User not logged in or cart is empty.");
                }

                var (success, message) = await _orderService.PlaceOrderAsync(userId, cartItems);

                if (!success)
                {
                    return BadRequest(message);
                }

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while placing the order. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

        // POST : Cancel Order Method
        [HttpPost]
		public async Task<IActionResult> CancelOrder(int orderId)
		{
            try
            {
                var (success, message) = await _orderService.CancelOrderAsync(orderId);

                if (!success)
                {
                    return BadRequest(message);
                }

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while canceling the order. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

        // POST : Order Again for Cancel Orders Method
        [HttpPost]
		public async Task<IActionResult> OrderAgain(int orderId)
		{
            try
            {
                var (success, message) = await _orderService.OrderAgainAsync(orderId);

                if (!success)
                {
                    return BadRequest(new { message });
                }

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while placing your order. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

        // POST : Change Order Status Method
        [HttpPost]
		public async Task<IActionResult> ChangeOrderStatus(int orderId, string newStatus)
		{
            try
            {
                var (success, message) = await _orderService.ChangeOrderStatusAsync(orderId, newStatus);

                if (!success)
                {
                    return BadRequest(message);
                }

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while changing the order status. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

        // GET : Get Order Details By ID
        [HttpGet]
		public async Task<IActionResult> GetOrderDetails(int orderId)
		{

            try
            {
                var orderViewModel = await _orderService.GetOrderDetailsByIdAsync(orderId);

                if (orderViewModel == null)
                {
                    return NotFound("Order not found.");
                }

                return Json(orderViewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while fetching the order details. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

        // GET : Download Order Details By ID
        [HttpGet]
        public async Task<IActionResult> DownloadOrder(int orderId)
        {
            try
            {
                var pdfData = await _orderService.GenerateOrderPdfAsync(orderId);
                return File(pdfData, "application/pdf", $"Order_{orderId}.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while generating the PDF. Please try again later: {ex.Message}";
                return View("Error");
            }
        }
    }

}
