using System.ComponentModel.DataAnnotations.Schema;

namespace EBookStore.Models
{
    public class Order
    {
        public int OrderID { get; set; } 

        public int UserID { get; set; } 

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18, 2)")]
        public required decimal TotalAmount { get; set; }

        public required string OrderStatus { get; set; } //Pending,Shipped,Delivered


        // Navigation property: an Order belongs to a User
        public required User User { get; set; }

        // Navigation property: an Order can have multiple OrderDetails
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
