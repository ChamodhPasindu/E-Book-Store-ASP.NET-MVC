using Microsoft.AspNetCore.Mvc;
using EBookStore.services;
using Microsoft.AspNetCore.Authorization;
using EBookStore.Models.DTO;
using EBookStore.Models.Entity;

namespace EBookStore.Controllers
{
    public class AccountController : Controller
	{
        private readonly IAccountService _accountService;

		public AccountController( IAccountService accountService)
		{
            _accountService = accountService;
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
                var users = _accountService.GetActiveCustomers();
                return View(users);
            }
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
                var deliveredOrderViewModels = await _accountService.GetDeliveredOrdersAsync(fromDate, toDate);
                return View(deliveredOrderViewModels);
            }
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
                    var isRegistered = await _accountService.RegisterCustomerAsync(newUser);
                    if (isRegistered)
                    {
                        TempData["SuccessMessage"] = "Account Created Successfully!";
                        return RedirectToAction("Login");
                    }
                }
                return View(newUser);
            }
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
                    var isRegistered = await _accountService.RegisterAdminAsync(newUser);
                    if (isRegistered)
                    {
                        TempData["SuccessMessage"] = "Admin Account Created Successfully!";
                        return View("Setting");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Admin Account Creation Failed!";
                        return View("Setting");
                    }
                }
                TempData["ErrorMessage"] = "Invalid data provided!";
                return View("Setting");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating admin account. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

		// POST: Login Method
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(string email, string password)
		{
            try
            {
                var (isSuccess, user, errorMessage) = await _accountService.LoginAsync(email, password);

                if (isSuccess && user != null)
                {
                    // Store user in session
                    HttpContext.Session.SetString("Email", user.Email);
                    HttpContext.Session.SetString("UserID", user.UserID.ToString());
                    HttpContext.Session.SetString("FirstName", user.FirstName);
                    HttpContext.Session.SetString("LastName", user.LastName);
                    HttpContext.Session.SetString("Address", user.Address);
                    HttpContext.Session.SetString("Role", user.Role);

                    // Redirect based on role
                    return user.Role == "Admin" ? RedirectToAction("AdminDashboard", "Home") : RedirectToAction("Index", "Home");
                }

                ViewBag.Error = errorMessage;
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while logging in. Please try again later: {ex.Message}";
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
                var user = await _accountService.GetCustomerByIdAsync(id.Value);

                if (user == null)
                {
                    return NotFound();
                }

                return Json(user); // Return the user details as JSON for use in the modal or edit form
            }
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
                var success = await _accountService.DeleteUserAsync(id);
                return Json(new { success });
            }
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
                    var success = await _accountService.UpdateUserDetailsAsync(model);
                    if (!success)
                    {
                        return NotFound(); // User not found
                    }

                    TempData["SuccessMessage"] = "Details updated successfully!";
                    return View("Setting");
                }
                else
                {
                    TempData["ErrorMessage"] = "Details update failed!";
                    return View("Setting");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating user details. Please try again later: {ex.Message}";
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

                var success = await _accountService.ChangePasswordAsync(userId, currentPassword, newPassword);
                if (!success)
                {
                    TempData["ErrorMessage"] = "Current password is incorrect.";
                    return RedirectToAction("Setting");
                }

                TempData["SuccessMessage"] = "Password updated successfully!";
                return RedirectToAction("Setting");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while changing the user password. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

        // GET : Download All Customers
        [HttpGet]
        public async Task<IActionResult> DownloadCustomers()
        {
            try
            {
                var pdfBytes = await _accountService.DownloadCustomersAsync();
                return File(pdfBytes, "application/pdf", "AllCustomers.pdf");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while downloading customers. Please try again later: {ex.Message}";
                return View("Error");
            }
        }
	}
}
