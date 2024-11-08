using System.Net;
using EBookStore.Models;
using EBookStore.Models.DTO;
using EBookStore.Models.Entity;
using Microsoft.EntityFrameworkCore;
using PdfWriter = iText.Kernel.Pdf.PdfWriter;

namespace EBookStore.services.Impl
{
	public class BookService : IBookService
	{
		private readonly BookStoreContext _context;

		public BookService(BookStoreContext context)
		{
			_context = context;
		}

		public async Task<List<Book>> GetActiveBooksAsync()
		{
			return await _context.Books
				.Where(b => b.IsActive)
				.ToListAsync();
		}

		public async Task<List<Book>> GetActiveBooksWithFeedbackAsync()
		{
			return await _context.Books
				.Where(b => b.IsActive)
				.Include(b => b.FeedBacks) // Include feedbacks
				.ToListAsync();
		}

		public async Task<BookDetailViewModel> GetBookDetailAsync(int id, int? userId)
		{
			var book = await _context.Books
				.Include(b => b.FeedBacks) // Include feedback related to the book
					.ThenInclude(f => f.User) // Include the user who left the feedback
				.FirstOrDefaultAsync(b => b.BookID == id);

			if (book == null)
			{
				return null; // Return null if the book is not found
			}

			bool hasPurchasedBook = false;

			if (userId.HasValue)
			{
				// Check if the logged-in user has purchased the book
				hasPurchasedBook = await _context.Orders
					.AnyAsync(o => o.Id == userId.Value && o.OrderDetails.Any(oi => oi.BookID == id));
			}

			// Prepare the view model
			var model = new BookDetailViewModel
			{
				Book = book,
				Feedbacks = book.FeedBacks,
				HasPurchasedBook = hasPurchasedBook
			};

			return model;
		}

		public async Task<Book?> GetBookByIdAsync(int id)
		{
			return await _context.Books
				.Where(b => b.BookID == id)
				.Include(b => b.FeedBacks)
				.Select(b => new Book
				{
					BookID = b.BookID,
					Title = b.Title,
					Author = b.Author,
					Category = b.Category,
					Price = b.Price,
					QuantityInStock = b.QuantityInStock,
					PublicationDate = b.PublicationDate,
					Description = b.Description,
					FeedBacks = b.FeedBacks,
					ImageMimeType = b.ImageMimeType,
					ImageData = b.ImageData
				}).FirstOrDefaultAsync();

		}

		public async Task AddOrEditBookAsync(Book book, IFormFile imageFile)
		{
			if (book.BookID == 0)
			{
				// Add new book
				if (imageFile != null && imageFile.Length > 0)
				{
					using (var ms = new MemoryStream())
					{
						await imageFile.CopyToAsync(ms);
						book.ImageData = ms.ToArray(); // Save image as byte array
						book.ImageMimeType = imageFile.ContentType; // Save the MIME type
					}
				}

				_context.Books.Add(book);
			}
			else
			{
				// Edit existing book
				var existingBook = await _context.Books.FindAsync(book.BookID);
				if (existingBook != null)
				{
					existingBook.Title = book.Title;
					existingBook.Author = book.Author;
					existingBook.Category = book.Category;
					existingBook.Price = book.Price;
					existingBook.QuantityInStock = book.QuantityInStock;
					existingBook.PublicationDate = book.PublicationDate;

					if (imageFile != null && imageFile.Length > 0)
					{
						using (var ms = new MemoryStream())
						{
							await imageFile.CopyToAsync(ms);
							existingBook.ImageData = ms.ToArray(); // Save image as byte array
							existingBook.ImageMimeType = imageFile.ContentType; // Save the MIME type
						}
					}
				}
			}

			await _context.SaveChangesAsync();
		}

		public async Task<bool> DeleteBookAsync(int id)
		{
			var book = await _context.Books.FindAsync(id);
			if (book != null)
			{
				book.IsActive = false; // Mark the book as inactive
				_context.Books.Update(book);
				await _context.SaveChangesAsync();
				return true; // Indicate success
			}
			return false; // Indicate failure
		}

		public async Task<bool> AddReviewAsync(int bookId, int userId, string feedbackText, int rating)
		{
			var user = await _context.Users.FindAsync(userId);
			var book = await _context.Books.FindAsync(bookId);

			if (user != null && book != null)
			{
				var newFeedback = new FeedBack
				{
					BookID = bookId,
					Id = userId,
					FeedbackText = feedbackText,
					Rating = rating,
					FeedbackDate = DateTime.Now,
					User = user,
					Book = book
				};

				_context.FeedBacks.Add(newFeedback);
				await _context.SaveChangesAsync();
				return true; // Indicate success
			}
			return false; // Indicate failure
		}

		public async Task<byte[]> GenerateBooksPdfAsync()
		{
			var books = await _context.Books.Where(b => b.IsActive).ToListAsync();

			using (var stream = new MemoryStream())
			{
				var writer = new PdfWriter(stream);
				var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
				var document = new iText.Layout.Document(pdf);

				document.Add(new iText.Layout.Element.Paragraph("Books Report").SetFontSize(18).SetBold());

				var table = new iText.Layout.Element.Table(5);
				table.AddHeaderCell("Title");
				table.AddHeaderCell("Author");
				table.AddHeaderCell("Category");
				table.AddHeaderCell("Publication Date");
				table.AddHeaderCell("Price");

				foreach (var book in books)
				{
					table.AddCell(book.Title);
					table.AddCell(book.Author);
					table.AddCell(book.Category);
					table.AddCell(book.PublicationDate.ToString("yyyy-MM-dd")); // Adjusting format for clarity
					table.AddCell(book.Price.ToString("C", new System.Globalization.CultureInfo("en-LK")));
				}
				document.Add(table);
				document.Close();

				return stream.ToArray(); // Return PDF as byte array
			}
		}
	}
}
