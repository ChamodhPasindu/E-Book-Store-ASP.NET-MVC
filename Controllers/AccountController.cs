using EBookStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public IActionResult Login()
        {
            return View();
        }

        // GET: Logout View
        public IActionResult Logout()
        {
            // Clear session
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // GET: CustomerManagement View With All Customers
        public IActionResult CustomerManagement()
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

        // POST: Register Method
        [HttpPost]
        [Route("account/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User newUser)
        {
            if (ModelState.IsValid)
            {
                // Hash the password
                newUser.Password = _passwordHasher.HashPassword(newUser, newUser.Password);

                // Save the new user with hashed password to the database
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                return RedirectToAction("Login");
            }
            return View(newUser);
        }

        // POST: Login Method
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
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

		// GET: Customer By ID
		public async Task<IActionResult> GetCustomer(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var user = await _context.Users
				.FirstOrDefaultAsync(m => m.UserID == id);
			if (user == null)
			{
				return NotFound();
			}

			return Json(user);// Return the user details as JSON for use in the modal or edit form
		}

		// POST: Customer Delete By ID
		[HttpPost]
		public async Task<IActionResult> Delete(int id)
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

	}
}
