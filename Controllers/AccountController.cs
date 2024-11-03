using EBookStore.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System.IO;
using PdfWriter = iText.Kernel.Pdf.PdfWriter;


namespace EBookStore.Controllers
{
	public class AccountController : Controller
	{
		private readonly BookStoreContext _context;
		private readonly IPasswordHasher<User> _passwordHasher;

		public AccountController(BookStoreContext context, IPasswordHasher<User> passwordHasher)
		{
			_context = context;
			_passwordHasher = passwordHasher;
		}

		// GET: Register View
		[HttpGet]
		public IActionResult Register()
		{
			return View();
		}

		// GET: Login View
		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}

		// GET: Logout View
		[HttpGet]
		public IActionResult Logout()
		{
			// Clear session
			HttpContext.Session.Clear();
			return RedirectToAction("Login");
		}

		// GET: CustomerManagement View With All Customers
		[HttpGet]
		public IActionResult CustomerManagement()
		{
			try
			{
				var users = _context.Users
							  .Where(b => b.IsActive && b.Role == "Customer") // Only select active books
							  .ToList();
				if (users == null)
				{
					// Handle case where no users are found
					return View(new List<User>());
				}
				return View(users);
			}
			//handle exceptions
			catch (Exception ex)
			{
				TempData["ErrorMessage"] = $"An error occurred while retrieving the customer list: {ex.Message}";
				return View("Error");
			}
		}

		// GET: Settings View
		[HttpGet]
		public IActionResult Setting()
		{
			return View();
		}

		// GET : Report Page With Complete Orders Data
		[HttpGet]
		public async Task<IActionResult> Report(DateTime? fromDate, DateTime? toDate)
		{
			try
			{
				var query = _context.Orders
				.Include(o => o.User)
				.Where(o => o.OrderStatus == "Delivered");

				// Apply date filtering
				if (fromDate.HasValue)
					query = query.Where(o => o.OrderDate >= fromDate.Value);
				if (toDate.HasValue)
					query = query.Where(o => o.OrderDate <= toDate.Value);

				var deliveredOrders = await query.ToListAsync();

				// Map to OrderViewModel
				var deliveredOrderViewModels = deliveredOrders.Select(o => new OrderViewModel
				{
					OrderID = o.OrderID,
					OrderStatus = o.OrderStatus,
					OrderDate = o.OrderDate,
					TotalAmount = o.TotalAmount,
					CustomerName = $"{o.User.FirstName} {o.User.LastName}",
					CustomerEmail = o.User.Email,
					CustomerAddress = o.User.Address,
					OrderItems = o.OrderDetails.Select(od => new OrderItemViewModel
					{
						BookTitle = od.Book.Title,
						Quantity = od.Quantity,
						Price = od.Price,
						ImageData = $"data:{od.Book.ImageMimeType};base64,{Convert.ToBase64String(od.Book.ImageData)}",
					}).ToList()
				}).ToList();

				// Pass the list to the view as the model
				return View(deliveredOrderViewModels);
			}
			//handle exceptions
			catch (Exception ex)
			{

				TempData["ErrorMessage"] = $"An error occurred while generating the report. Please try again later: {ex.Message}";
				return View("Error");
			}
		}

		// POST: Register Method
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RegisterCustomer(User newUser)
		{
			try
			{
				if (ModelState.IsValid)
				{
					// Hash the password
					newUser.Password = _passwordHasher.HashPassword(newUser, newUser.Password);

					// Save the new user with hashed password to the database
					_context.Users.Add(newUser);
					await _context.SaveChangesAsync();
					TempData["SuccessMessage"] = "Account Created Successfully!";
					return RedirectToAction("Login");
				}
				return View(newUser);
			}
			//handle exceptions
			catch (Exception ex)
			{

				TempData["ErrorMessage"] = $"An error occurred while registering customer request. Please try again later: {ex.Message}";
				return View("Error");
			}
		}

		// POST: Register Method
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RegisterAdmin(User newUser)
		{
			try
			{
				if (ModelState.IsValid)
				{
					// Hash the password
					newUser.Password = _passwordHasher.HashPassword(newUser, newUser.Password);

					newUser.Role = "Admin";
					_context.Users.Add(newUser);
					await _context.SaveChangesAsync();
					TempData["SuccessMessage"] = "Admin Account Created Successfully!";
					return View("Setting");
				}
				else
				{
					TempData["ErrorMessage"] = "Admin Account Created Failed!";
					return View("Setting");
				}
			}
			//handle exceptions
			catch (Exception ex)
			{

				TempData["ErrorMessage"] = $"An error occurred while creating admin account. Please try again later: {ex.Message}";
				return View("Error");
			}
		}

		// POST: Login Method
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Login(string email, string password)
		{
			try
			{
				var user = _context.Users.FirstOrDefault(u => u.Email == email);
				if (user != null)
				{
					var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);

					if (result == PasswordVerificationResult.Success)
					{
						// Store user in session (for simplicity)
						HttpContext.Session.SetString("Email", user.Email);
						HttpContext.Session.SetString("UserID", user.UserID.ToString());
						HttpContext.Session.SetString("FirstName", user.FirstName);
						HttpContext.Session.SetString("LastName", user.LastName);
						HttpContext.Session.SetString("Address", user.Address);
						HttpContext.Session.SetString("Role", user.Role);

						// Redirect based on role
						if (user.Role == "Admin")
						{
							return RedirectToAction("AdminDashboard", "Home");
						}
						else
						{
							return RedirectToAction("Index", "Home");
						}
					}
					else
					{
						ViewBag.Error = "Incorrect Password!";
						return View();
					}
				}

				ViewBag.Error = "Invalid login attempt!";
				return View();
			}
			//handle exceptions
			catch (Exception ex)
			{

				TempData["ErrorMessage"] = $"An error occurred while login. Please try again later: {ex.Message}";
				return View("Error");
			}
		}

		// GET: Customer By ID
		[HttpGet]
		public async Task<IActionResult> GetCustomer(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			try
			{
				var user = await _context.Users
				.Where(b => b.UserID == id)
				.Include(b => b.FeedBacks).
				Select(b => new
				{
					b.UserID,
					b.FirstName,
					b.LastName,
					b.Email,
					b.PhoneNumber,
					b.RegistrationDate,
					b.Address,
					b.FeedBacks,
				}).FirstOrDefaultAsync(m => m.UserID == id);
				if (user == null)
				{
					return NotFound();
				}

				return Json(user);// Return the user details as JSON for use in the modal or edit form
			}
			//handle exceptions
			catch (Exception ex)
			{

				TempData["ErrorMessage"] = $"An error occurred while retrieving the customer. Please try again later: {ex.Message}";
				return View("Error");
			}
		}

		// POST: Customer Delete By ID
		[HttpPost]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				var user = await _context.Users.FindAsync(id);
				if (user != null)
				{
					user.IsActive = false;
					_context.Users.Update(user);
					await _context.SaveChangesAsync();
					return Json(new { success = true });
				}
				return Json(new { success = false });
			}
			//handle exceptions
			catch (Exception ex)
			{

				TempData["ErrorMessage"] = $"An error occurred while deleting the customer. Please try again later: {ex.Message}";
				return View("Error");
			}
		}

		// POST : Update User Details Method
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateDetails(UserSettingsViewModel model)
		{
			try
			{
				if (ModelState.IsValid)
				{
					var user = await _context.Users.FindAsync(model.UserID);
					if (user == null)
					{
						return NotFound();
					}

					// Update user details
					user.FirstName = model.FirstName;
					user.LastName = model.LastName;
					user.Email = model.Email;
					user.Address = model.Address;

					await _context.SaveChangesAsync();

					TempData["SuccessMessage"] = "Details updated successfully!";
					return View("Setting");
				}
				else
				{
					TempData["ErrorMessage"] = "Details updated Failed!";
					return View("Setting");
				}
			}
			//handle exceptions
			catch (Exception ex)
			{

				TempData["ErrorMessage"] = $"An error occurred while updating user requeste. Please try again later: {ex.Message}";
				return View("Error");
			}
		}

		// POST : Change Password Method
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
		{
			try
			{
				var userId = HttpContext.Session.GetString("UserID");
				if (userId == null)
				{
					return RedirectToAction("Login", "Account");
				}

				var user = await _context.Users.FindAsync(int.Parse(userId));
				if (user == null)
				{
					return NotFound();
				}

				// Verify current password
				var result = _passwordHasher.VerifyHashedPassword(user, user.Password, currentPassword);

				if (result != PasswordVerificationResult.Success)
				{
					TempData["ErrorMessage"] = "Current password is incorrect.";
					return RedirectToAction("Setting");
				}


				// Update password
				user.Password = _passwordHasher.HashPassword(user, newPassword);
				await _context.SaveChangesAsync();

				TempData["SuccessMessage"] = "Password updated successfully!";
				return RedirectToAction("Setting");
			}
			//handle exceptions
			catch (Exception ex)
			{

				TempData["ErrorMessage"] = $"An error occurred while changing the user password. Please try again later: {ex.Message}";
				return View("Error");
			}
		}

		// GET : Download Order Details By ID
		[HttpGet]
		public async Task<IActionResult> DownloadOrder(int orderId)
		{
			try
			{
				var order = await _context.Orders
				.Include(o => o.OrderDetails)
				.ThenInclude(od => od.Book)
				.Include(o => o.User)
				.FirstOrDefaultAsync(o => o.OrderID == orderId);

				if (order == null) return NotFound();

				using (var stream = new MemoryStream())
				{
					// Initialize the PDF writer
					var writer = new PdfWriter(stream);
					var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
					var document = new iText.Layout.Document(pdf);

					// Add title
					document.Add(new iText.Layout.Element.Paragraph($"Order Report - Order ID: {orderId}").SetFontSize(18).SetBold());

					// Add customer details
					document.Add(new iText.Layout.Element.Paragraph($"Customer: {order.User.FirstName} {order.User.LastName}"));
					document.Add(new iText.Layout.Element.Paragraph($"Email: {order.User.Email}"));
					document.Add(new iText.Layout.Element.Paragraph($"Address: {order.User.Address}"));
					document.Add(new iText.Layout.Element.Paragraph($"Order Date: {order.OrderDate.ToString("yyyy-MM-dd")}"));
					document.Add(new iText.Layout.Element.Paragraph($"Total Amount: {order.TotalAmount.ToString("C", new System.Globalization.CultureInfo("en-LK"))}"));

					// Add order item details
					document.Add(new iText.Layout.Element.Paragraph("Order Items:"));
					var table = new iText.Layout.Element.Table(3); // 3 columns for Book Title, Quantity, and Price
					table.AddHeaderCell("Book Title");
					table.AddHeaderCell("Quantity");
					table.AddHeaderCell("Price");

					foreach (var item in order.OrderDetails)
					{
						table.AddCell(item.Book.Title);
						table.AddCell(item.Quantity.ToString());
						table.AddCell(item.Price.ToString("C", new System.Globalization.CultureInfo("en-LK")));
					}
					document.Add(table);

					// Close the document
					document.Close();

					return File(stream.ToArray(), "application/pdf", $"Order_{orderId}.pdf");
				}
			}
			//handle exceptions
			catch (Exception ex)
			{

				TempData["ErrorMessage"] = $"An error occurred while changing the user password. Please try again later: {ex.Message}";
				return View("Error");
			}
		}

		// GET : Download All Books
		[HttpGet]
		public async Task<IActionResult> DownloadBooks()
		{
			try
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
						table.AddCell(book.PublicationDate.ToString().Split('T')[0]);
						table.AddCell(book.Price.ToString("C", new System.Globalization.CultureInfo("en-LK")));
					}
					document.Add(table);

					document.Close();

					return File(stream.ToArray(), "application/pdf", "AllBooks.pdf");
				}
			}
			//handle exceptions
			catch (Exception ex)
			{

				TempData["ErrorMessage"] = $"An error occurred while downloading books. Please try again later: {ex.Message}";
				return View("Error");
			}
		}

		// GET : Download All Customers
		[HttpGet]
		public async Task<IActionResult> DownloadCustomers()
		{
			try
			{
				var customers = await _context.Users.Where(b => b.IsActive).ToListAsync();

				using (var stream = new MemoryStream())
				{
					var writer = new PdfWriter(stream);
					var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
					var document = new iText.Layout.Document(pdf);

					document.Add(new iText.Layout.Element.Paragraph("Customers Report").SetFontSize(18).SetBold());

					var table = new iText.Layout.Element.Table(5);
					table.AddHeaderCell("Name");
					table.AddHeaderCell("Email");
					table.AddHeaderCell("Phone Number");
					table.AddHeaderCell("Address");
					table.AddHeaderCell("Registration Date");

					foreach (var customer in customers)
					{
						table.AddCell($"{customer.FirstName} {customer.LastName}");
						table.AddCell(customer.Email);
						table.AddCell(customer.PhoneNumber);
						table.AddCell(customer.Address);
						table.AddCell(customer.RegistrationDate.ToString().Split('T')[0]);
					}
					document.Add(table);

					document.Close();

					return File(stream.ToArray(), "application/pdf", "AllCustomers.pdf");
				}
			}
			//handle exceptions
			catch (Exception ex)
			{

				TempData["ErrorMessage"] = $"An error occurred while downloading customers. Please try again later: {ex.Message}";
				return View("Error");
			}
		}
	}
}
