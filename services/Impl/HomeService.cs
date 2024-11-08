using EBookStore.Models.DTO;
using EBookStore.Models.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EBookStore.services.Impl
{
	public class HomeService : IHomeService
	{
		private readonly BookStoreContext _context;
		private readonly UserManager<User> _userManager;
		public HomeService(BookStoreContext context, UserManager<User> userManager)
		{
			_context = context;
			_userManager = userManager;
		}

		public async Task<AdminDashboardViewModel> GetDashboardDataAsync()
		{
			var activeUsers = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
			var totalActiveCustomers = activeUsers.Count(u => _userManager.IsInRoleAsync(u, "Customer").Result);
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

			var recentActiveUsers = await _userManager.Users
				.Where(u => u.IsActive)
				.OrderByDescending(u => u.RegistrationDate)
				.Take(5)
				.ToListAsync();

			var customerUsers = new List<User>();

			foreach (var user in recentActiveUsers)
			{
				// Retrieve the roles for each user
				var roles = await _userManager.GetRolesAsync(user);

				if (roles.Contains("Customer"))
				{
					// Map to your custom User model
					customerUsers.Add(new User
					{
						Id = user.Id,
						FirstName = user.FirstName,
						LastName = user.LastName,
						Email = user.Email,
						PhoneNumber = user.PhoneNumber,
						RegistrationDate = user.RegistrationDate,
						Address = user.Address,
						FeedBacks = user.FeedBacks,
						Password = null
						/*Role = roles.FirstOrDefault()*/ // You can assign the first role if they have one
					});
				}
			}

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
				TotalUsers = totalActiveCustomers,
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
