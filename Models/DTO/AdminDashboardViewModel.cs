using EBookStore.Models.Entity;

namespace EBookStore.Models.DTO
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalBooks { get; set; }
        public int PendingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int CanceledOrders { get; set; }
        public decimal DailyEarnings { get; set; }
        public decimal WeeklyEarnings { get; set; }
        public decimal YearlyEarnings { get; set; }
        public decimal MonthlyEarnings { get; set; }

        public List<Order> RecentOrders { get; set; } = new List<Order>();
        public List<User> RecentUsers { get; set; } = new List<User>();
    }
}
