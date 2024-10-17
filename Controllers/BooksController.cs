using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EBookStore.Models;

namespace EBookStore.Controllers
{
	public class BooksController : Controller
	{
		private readonly BookStoreContext _context;

		public BooksController(BookStoreContext context)
		{
			_context = context;
		}

        // GET: BookManagement
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

        // GET: BookStore
        public IActionResult BookStore()
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

        // GET: Books/GetBook/5
        public async Task<IActionResult> GetBook(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var book = await _context.Books
				.Where(b => b.BookID == id)
				.Select(b => new
				{
					b.BookID,
					b.Title,
					b.Author,
					b.Category,
					b.Price,
					b.QuantityInStock,
					b.PublicationDate,
					ImageData = b.ImageData != null ? Convert.ToBase64String(b.ImageData) : null, // Convert to base64 string
					b.ImageMimeType
				}).FirstOrDefaultAsync(m => m.BookID == id);
			if (book == null)
			{
				return NotFound();
			}

			return Json(book);// Return the book details as JSON for use in the modal or edit form
		}

		// POST: Books/AddOrEdit
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
			return RedirectToAction("BookManagement");
		}

		// POST: Books/Delete/5
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
	}
}
