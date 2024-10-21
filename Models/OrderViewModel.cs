namespace EBookStore.Models
{
	public class OrderViewModel
	{
		public int OrderID { get; set; }
		public required string OrderStatus { get; set; }
		public DateTime OrderDate { get; set; }
		public decimal TotalAmount { get; set; }
		public required string CustomerName { get; set; }
		public required string CustomerEmail { get; set; }
		public required string CustomerAddress{ get; set; }
		public required List<OrderItemViewModel> OrderItems { get; set; }
	}

	public class OrderItemViewModel
	{
		public required string BookTitle { get; set; }
		public int Quantity { get; set; }
		public decimal Price { get; set; }
	}
}
