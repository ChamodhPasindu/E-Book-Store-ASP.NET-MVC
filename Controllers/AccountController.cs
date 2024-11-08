using Microsoft.AspNetCore.Mvc;
using EBookStore.services;
using Microsoft.AspNetCore.Authorization;
using EBookStore.Models.DTO;
using EBookStore.Models.Entity;
using Microsoft.AspNetCore.Identity;

namespace EBookStore.Controllers
{
	public class AccountController : Controller
	{
		private readonly IAccountService _accountService; 
		private readonly SignInManager<User> _signInManager;
		private readonly UserManager<User> _userManager;

		public AccountController(IAccountService accountService, SignInManager<User> signInManager, UserManager<User> userManager)
		{
			_accountService = accountService;
			_signInManager = signInManager;
			_userManager = userManager;
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
		public async Task<IActionResult> Logout()
		{
			try{
				await _signInManager.SignOutAsync();
				//HttpContext.Session.Clear();
				return RedirectToAction("Login", "Account");
			}
			catch (Exception ex)
			{
				TempData["ErrorMessage"] = $"An error occurred while Loging . Please try again later: {ex.Message}";
				return View("Error");
			}
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
		[Route("Account/RegisterCustomer")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RegisterCustomer(User newUser)
		{
			try
			{
				newUser.UserName = newUser.Email;
				var result = await _userManager.CreateAsync(newUser, newUser.Password);
				if (result.Succeeded)
				{
					await _userManager.AddToRoleAsync(newUser, "Customer");
					TempData["SuccessMessage"] = "Account Created Successfully!";
					return RedirectToAction("Login", "Account");
				}
				else
				{
					foreach (var error in result.Errors)
					{
						throw new Exception(error.Description);
					}
					return View("Register", newUser);
				}
				
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
				// Attempt to sign in the user with SignInManager
				var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);

				if (result.Succeeded)
				{
					// Fetch the user to check the role (optional, depending on redirection logic)
					var user = await _userManager.FindByEmailAsync(email);
					if (user != null)
					{
						// Redirect based on user role without storing in session
						if (await _userManager.IsInRoleAsync(user, "Admin"))
						{
							return RedirectToAction("AdminDashboard", "Home");
						}
						return RedirectToAction("Index", "Home");
					}
				}

				// If login failed, show error message
				ViewBag.Error = "Invalid login attempt.";
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
