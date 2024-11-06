using EBookStore.Models.DTO;
using EBookStore.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace EBookStore.services.Impl
{
    public class HomeService : IHomeService
    {
        private readonly BookStoreContext _context;
        public HomeService(BookStoreContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardViewModel> GetDashboardDataAsync()
        {
            // Fetch the total counts of active entities
            var totalActiveUsers = await _context.Users.CountAsync(u => u.IsActive && u.Role == "Customer");
            var totalActiveBooks = await _context.Books.CountAsync(b => b.IsActive);
            var totalPendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Pending");
            var totalDeliveredOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Delivered");
            var totalShippedOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Shipped");
            var totalCanceledOrders = await _context.Orders.CountAsync(o => o.OrderStatus == "Canceled");

            var recentActiveOrders = await _context.Orders
                .Include(o => o.User)
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

            var dailyEarnings = await _context.Orders
                .Where(o => o.OrderDate >= today && o.OrderStatus == "Delivered")
                .SumAsync(o => o.TotalAmount);

            var weeklyEarnings = await _context.Orders
                .Where(o => o.OrderDate >= firstDayOfWeek && o.OrderStatus == "Delivered")
                .SumAsync(o => o.TotalAmount);

            var monthlyEarnings = await _context.Orders
                .Where(o => o.OrderDate >= firstDayOfMonth && o.OrderStatus == "Delivered")
                .SumAsync(o => o.TotalAmount);

            var yearlyEarnings = await _context.Orders
                .Where(o => o.OrderDate >= firstDayOfYear && o.OrderStatus == "Delivered")
                .SumAsync(o => o.TotalAmount);

            return new AdminDashboardViewModel
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
        }
    }
}
