using Microsoft.AspNetCore.Mvc;
using EBookStore.services;
using Microsoft.AspNetCore.Authorization;
using EBookStore.Models.Entity;

namespace EBookStore.Controllers
{
    public class BooksController : Controller
	{
		private readonly IBookService _bookService;

		public BooksController(IBookService bookService)
		{
			_bookService = bookService;
		}

        // GET: BookManagement View With All Books
        [HttpGet]
		public async Task<IActionResult> BookManagement()
		{
            try
            {
                var books = await _bookService.GetActiveBooksAsync();
                if (books == null || !books.Any())
                {
                    // Handle case where no books are found
                    return View(new List<Book>());
                }
                return View(books);
            }
            // Handle exceptions
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while retrieving the book list. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

		// GET: BookStore View With All Books
		[HttpGet]
		public async Task<IActionResult> BookStore()
		{
            try
            {
                var books = await _bookService.GetActiveBooksWithFeedbackAsync();
                if (books == null || !books.Any())
                {
                    // Handle case where no books are found
                    return View(new List<Book>());
                }
                return View(books);
            }
            // Handle exceptions
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while retrieving the book list. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

		// GET: BookDetail View By ID
		[HttpGet]
		public async Task<IActionResult> BookDetail(int id)
		{
            try
            {
                var userId = HttpContext.Session.GetString("UserID");
                int? currentUserId = string.IsNullOrEmpty(userId) ? (int?)null : int.Parse(userId);

                var model = await _bookService.GetBookDetailAsync(id, currentUserId);

                if (model == null)
                {
                    return NotFound();
                }

                // Return the view with the model
                return View(model);
            }
            // Handle exceptions
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while fetching book detail. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

		// GET: Book By ID
		[HttpGet]
		public async Task<IActionResult> GetBook(int? id)
		{
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                var book = await _bookService.GetBookByIdAsync(id.Value);

                if (book == null)
                {
                    return NotFound();
                }

                return Json(book); // Return the book details as JSON for use in the modal or edit form
            }
            // Handle exceptions
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while fetching book detail. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

        // POST: Add & Edit Method
        [HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddOrEdit(Book book, IFormFile imageFile)
		{
            try
            {
                await _bookService.AddOrEditBookAsync(book, imageFile);
                TempData["SuccessMessage"] = book.BookID == 0 ? "Book added successfully" : "Book edited successfully";
                return RedirectToAction("BookManagement");
            }
            // Handle exceptions
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while adding or editing the book: {ex.Message}";
                return View("Error");
            }
        }

        // POST: Book Delete By ID
        [HttpPost]
		public async Task<IActionResult> Delete(int id)
		{
            try
            {
                var result = await _bookService.DeleteBookAsync(id);
                return Json(new { success = result });
            }
            // Handle exceptions
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the book. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

        // POST : Add Review To Book
        [HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddReview(int bookId, string feedbackText, int rating)
		{
            try
            {
                var userId = HttpContext.Session.GetString("UserID");

                // Check if user is logged in
                if (!string.IsNullOrEmpty(userId))
                {
                    bool isAdded = await _bookService.AddReviewAsync(bookId, int.Parse(userId), feedbackText, rating);

                    if (isAdded)
                    {
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
            // Handle exceptions
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while adding review. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

        // GET : Download All Books
        [HttpGet]
        public async Task<IActionResult> DownloadBooks()
        {
            try
            {
                var pdfBytes = await _bookService.GenerateBooksPdfAsync();
                return File(pdfBytes, "application/pdf", "AllBooks.pdf");
            }
            // Handle exceptions
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while downloading books. Please try again later: {ex.Message}";
                return View("Error");
            }
        }
    }
}
