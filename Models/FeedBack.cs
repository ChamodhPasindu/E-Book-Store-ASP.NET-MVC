namespace EBookStore.Models
{
    public class FeedBack
    {
        public int FeedBackID { get; set; }

        public int UserID { get; set; }

        public int BookID { get; set; }

        public required string FeedbackText { get; set; }

        public int Rating { get; set; }

        public DateTime FeedbackDate { get; set; } = DateTime.Now;

        // Navigation properties
        public required User User { get; set; }

        public required Book Book { get; set; }
    }
}
