using EBookStore.Models.DTO;
using EBookStore.Models.Entity;

namespace EBookStore.services
{
    public interface IBookService
    {
        Task<List<Book>> GetActiveBooksAsync();

        Task<List<Book>> GetActiveBooksWithFeedbackAsync();

        Task<BookDetailViewModel> GetBookDetailAsync(int id, int? userId);

        Task<object> GetBookByIdAsync(int id);

        Task AddOrEditBookAsync(Book book, IFormFile imageFile);

        Task<bool> DeleteBookAsync(int id);

        Task<bool> AddReviewAsync(int bookId, int userId, string feedbackText, int rating);

        Task<byte[]> GenerateBooksPdfAsync();
    }
}
