namespace EBookStore.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalBooks { get; set; }
        public int PendingOrders { get; set; }

        public List<Order> RecentOrders { get; set; } = new List<Order>();
        public List<User> RecentUsers { get; set; } = new List<User>();
    }
}
