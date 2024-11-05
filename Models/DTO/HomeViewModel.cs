using EBookStore.Models.Entity;

namespace EBookStore.Models.DTO
{
    public class HomeViewModel
    {
        public required List<Book> Books { get; set; }
    }
}
