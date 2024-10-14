using System.ComponentModel.DataAnnotations.Schema;

namespace EBookStore.Models
{
    public class Book
    {
        public int BookID { get; set; } 

        public required string Title { get; set; }

        public required string Author { get; set; }

        public required string Category { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        public int QuantityInStock { get; set; }

        public DateTime PublicationDate { get; set; }

        public bool IsActive { get; set; } = true; 
    }
}
