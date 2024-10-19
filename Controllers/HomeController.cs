using System.Diagnostics;
using EBookStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EBookStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BookStoreContext _context;

        public HomeController(BookStoreContext context, ILogger<HomeController> logger)
        {
            _logger = logger;
            _context = context;
        }


        // GET : Home View 
        public IActionResult Index()
        {
            // Retrieve the cart item count from the session
            int cartItemCount = HttpContext.Session.GetInt32("CartItemCount") ?? 0;

            // Pass the count to the view via ViewData
            ViewData["CartItemCount"] = cartItemCount;

            return View();
        }

        // GET : Admin Dashboard View With Relevent Data
        public async Task<IActionResult> AdminDashboard()
        {
            // Fetch the total counts of active entities
            var totalActiveUsers = await _context.Users.CountAsync(u => u.IsActive && u.Role=="Customer");  // Assuming `IsActive` is present in the User model
            var totalActiveBooks = await _context.Books.CountAsync(b => b.IsActive);
            var totalActiveOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Pending"); // Example condition for Pending orders

            // Fetch recent active data (e.g., last 5 active orders and users)
            var recentActiveOrders = await _context.Orders
                .Include(o => o.User)  // Include user details in the orders
                .Where(o => o.OrderStatus == "Pending")
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
            .ToListAsync();

            var recentActiveUsers = await _context.Users
                .Where(u => u.IsActive && u.Role == "Customer")  // Assuming `IsActive` is present in the User model
                .OrderByDescending(u => u.RegistrationDate)
                .Take(5)
                .ToListAsync();

            // Prepare the ViewModel
            var model = new AdminDashboardViewModel
            {
                TotalUsers = totalActiveUsers,
                TotalOrders = totalActiveOrders,
                TotalBooks = totalActiveBooks,
                RecentOrders = recentActiveOrders,
                RecentUsers = recentActiveUsers
            };

            return View(model);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
