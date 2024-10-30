using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EBookStore.Models;
using Microsoft.AspNetCore.Identity;

namespace EBookStore.Controllers
{
	public class BooksController : Controller
	{
		private readonly BookStoreContext _context;

		public BooksController(BookStoreContext context)
		{
			_context = context;
		}

		// GET: BookManagement View With All Books
		[HttpGet]
		public IActionResult BookManagement()
		{
			var books = _context.Books
							  .Where(b => b.IsActive) // Only select active books
							  .ToList();
			if (books == null)
			{
				// Handle case where no books are found
				return View(new List<Book>());
			}
			return View(books);
		}

		// GET: BookStore View With All Books
		[HttpGet]
		public IActionResult BookStore()
		{
			var books = _context.Books
							  .Where(b => b.IsActive).Include(b => b.FeedBacks) // Only select active books
							  .ToList();
			if (books == null)
			{
				// Handle case where no books are found
				return View(new List<Book>());
			}
			return View(books);
		}

		// GET: BookDetail View By ID
		[HttpGet]
		public async Task<IActionResult> BookDetail(int id)
		{
			var book = await _context.Books
				.Include(b => b.FeedBacks) // Include feedback related to the book
				.ThenInclude(f => f.User) // Include the user who left the feedback
				.FirstOrDefaultAsync(b => b.BookID == id);

			if (book == null)
			{
				return NotFound();
			}

			var userId = HttpContext.Session.GetString("UserID");

			bool hasPurchasedBook = false;

			if (!string.IsNullOrEmpty(userId))
			{
				int currentUserId = int.Parse(userId);

				// Check if the logged-in user has purchased the book
				hasPurchasedBook = await _context.Orders
					.AnyAsync(o => o.UserID == currentUserId && o.OrderDetails.Any(oi => oi.BookID == id));
			}

			// Prepare the view model
			var model = new BookDetailViewModel
			{
				Book = book,
				Feedbacks = book.FeedBacks,
				HasPurchasedBook = hasPurchasedBook
			};

			// Return the view with the model
			return View(model);
		}

		// GET: Book By ID
		[HttpGet]
		public async Task<IActionResult> GetBook(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var book = await _context.Books
				.Where(b => b.BookID == id)
				.Include(b => b.FeedBacks)
				.Select(b => new
				{
					b.BookID,
					b.Title,
					b.Author,
					b.Category,
					b.Price,
					b.QuantityInStock,
					b.PublicationDate,
					b.Description,
					b.FeedBacks,
					ImageData = b.ImageData != null ? Convert.ToBase64String(b.ImageData) : null, // Convert to base64 string
					b.ImageMimeType
				}).FirstOrDefaultAsync(m => m.BookID == id);
			if (book == null)
			{
				return NotFound();
			}

			return Json(book);// Return the book details as JSON for use in the modal or edit form
		}

		// POST: Add & Edit Method
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult AddOrEdit(Book book, IFormFile ImageFile)
		{
			if (book.BookID == 0)
			{
				if (ImageFile != null && ImageFile.Length > 0)
				{
					using (var ms = new MemoryStream())
					{
						ImageFile.CopyTo(ms);
						book.ImageData = ms.ToArray();  // Save image as byte array
						book.ImageMimeType = ImageFile.ContentType;  // Save the MIME type
					}
				}

				// Add new book
				_context.Books.Add(book);
			}
			else
			{
				// Edit existing book
				var existingBook = _context.Books.Find(book.BookID);
				if (existingBook != null)
				{
					existingBook.Title = book.Title;
					existingBook.Author = book.Author;
					existingBook.Category = book.Category;
					existingBook.Price = book.Price;
					existingBook.QuantityInStock = book.QuantityInStock;
					existingBook.PublicationDate = book.PublicationDate;

					if (ImageFile != null && ImageFile.Length > 0)
					{
						using (var ms = new MemoryStream())
						{
							ImageFile.CopyTo(ms);
							existingBook.ImageData = ms.ToArray();  // Save image as byte array
							existingBook.ImageMimeType = ImageFile.ContentType;  // Save the MIME type
						}
					}
				}
			}
			_context.SaveChanges();
			TempData["SuccessMessage"] = book.BookID == 0 ? "Book added successfully" : "Book eddited successfully";
			return RedirectToAction("BookManagement");
		}

		// POST: Book Delete By ID
		[HttpPost]
		public async Task<IActionResult> Delete(int id)
		{
			var book = await _context.Books.FindAsync(id);
			if (book != null)
			{
				book.IsActive = false;
				_context.Books.Update(book);
				await _context.SaveChangesAsync();
				return Json(new { success = true });
			}
			return Json(new { success = false });
		}

		// POST : Add Review To Book
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddReview(int bookId, string feedbackText, int rating)
		{
			var userId = HttpContext.Session.GetString("UserID");

			// Check if user is logged in
			if (!string.IsNullOrEmpty(userId))
			{
				// Retrieve the user based on userId
				var user = await _context.Users.FindAsync(int.Parse(userId));

				// Retrieve the book based on bookId
				var book = await _context.Books.FindAsync(bookId);

				if (user != null && book != null) // Ensure both user and book exist
				{
					var newFeedback = new FeedBack
					{
						BookID = bookId,
						UserID = int.Parse(userId),
						FeedbackText = feedbackText,
						Rating = rating,
						FeedbackDate = DateTime.Now,
						User = user, // Set the User navigation property
						Book = book   // Set the Book navigation property
					};

					_context.FeedBacks.Add(newFeedback);
					await _context.SaveChangesAsync();

					// Redirect to the book detail page after successful feedback addition
					return RedirectToAction("BookDetail", new { id = bookId });
				}
				else
				{
					// Return an error message if the book or user is not found
					TempData["ErrorMessage"] = "Book not found or invalid user.";
					return RedirectToAction("BookDetail", new { id = bookId }); // Redirect back to the book detail page
				}
			}
			else
			{
				// Return an error message if the user is not logged in
				TempData["ErrorMessage"] = "You must be logged in to add a review.";
				return RedirectToAction("Login", "Account"); // Redirect to the login page or wherever appropriate
			}
		}
	}
}
