namespace EBookStore.Models
{
    public class BookDetailViewModel
    {
        public required Book Book { get; set; }
        public required IEnumerable<FeedBack> Feedbacks { get; set; }
        public bool HasPurchasedBook { get; set; }
    }
}
