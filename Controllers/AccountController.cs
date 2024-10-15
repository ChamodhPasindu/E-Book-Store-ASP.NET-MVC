using EBookStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
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

        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
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
                    HttpContext.Session.SetString("Role", user.Role);

                    // Redirect based on role
                    if (user.Role == "Admin")
                    {
                        return RedirectToAction("Index", "Admin");
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

        public IActionResult Logout()
        {
            // Clear session
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
