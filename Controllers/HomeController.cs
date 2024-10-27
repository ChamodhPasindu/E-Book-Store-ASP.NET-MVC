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
        [HttpGet]
        public IActionResult Index()
        {

			var books = _context.Books
							 .Where(b => b.IsActive) 
							 .ToList();
			if (books == null)
			{
				return View(new List<Book>());
			}
			return View(new HomeViewModel { Books = books });
		}

        // GET : Admin Dashboard View With Relevent Data
        [HttpGet]
        public async Task<IActionResult> AdminDashboard()
        {
            // Fetch the total counts of active entities
            var totalActiveUsers = await _context.Users.CountAsync(u => u.IsActive && u.Role=="Customer");  
            var totalActiveBooks = await _context.Books.CountAsync(b => b.IsActive);
            var totalPendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Pending"); 
            var totalDeliveredOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Delivered");
            var totalShippedOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Shipped"); 
            var totalCanceledOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Canceled");

            var recentActiveOrders = await _context.Orders
                .Include(o => o.User)  // Include user details in the orders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
            .ToListAsync();

            var recentActiveUsers = await _context.Users
                .Where(u => u.IsActive && u.Role == "Customer")  
                .OrderByDescending(u => u.RegistrationDate)
                .Take(5)
                .ToListAsync();

			var today = DateTime.Today;
			var firstDayOfWeek = today.AddDays(-(int)today.DayOfWeek);
			var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
			var firstDayOfYear = new DateTime(today.Year, 1, 1);

			// Daily Earnings (for today)
			var dailyEarnings = await _context.Orders
				.Where(o => o.OrderDate >= today && o.OrderStatus == "Delivered")
				.SumAsync(o => o.TotalAmount);

			// Weekly Earnings (from the start of the current week)
			var weeklyEarnings = await _context.Orders
				.Where(o => o.OrderDate >= firstDayOfWeek && o.OrderStatus == "Delivered")
				.SumAsync(o => o.TotalAmount);

			// Monthly Earnings (from the start of the current month)
			var monthlyEarnings = await _context.Orders
				.Where(o => o.OrderDate >= firstDayOfMonth && o.OrderStatus == "Delivered")
				.SumAsync(o => o.TotalAmount);

			// Yearly Earnings (from the start of the current year)
			var yearlyEarnings = await _context.Orders
				.Where(o => o.OrderDate >= firstDayOfYear && o.OrderStatus == "Delivered")
				.SumAsync(o => o.TotalAmount);

			// Prepare the ViewModel
			var model = new AdminDashboardViewModel
            {
                TotalUsers = totalActiveUsers,
                PendingOrders = totalPendingOrders,
                ShippedOrders = totalShippedOrders,
                DeliveredOrders = totalDeliveredOrders,
                CanceledOrders = totalCanceledOrders,
                TotalBooks = totalActiveBooks,

                RecentOrders = recentActiveOrders,
                RecentUsers = recentActiveUsers,

                DailyEarnings = dailyEarnings,
                WeeklyEarnings = weeklyEarnings,
                MonthlyEarnings = monthlyEarnings,
				YearlyEarnings = yearlyEarnings,
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
