using System.ComponentModel.DataAnnotations.Schema;

namespace EBookStore.Models
{
    public class OrderDetail
    {
        public int OrderDetailID { get; set; }  

        public int OrderID { get; set; } 

        public int BookID { get; set; }  

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        // Navigation properties
        public  required Order Order { get; set; }

        public required Book Book { get; set; }
    }
}
