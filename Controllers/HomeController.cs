using EBookStore.Models.Entity;
using EBookStore.Models.DTO;
using EBookStore.services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBookStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;
        private readonly IBookService _bookService;

        public HomeController(IHomeService homeService, IBookService bookService)
        {
            _bookService = bookService;
            _homeService = homeService;
        }

        // GET : Home View 
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var books = await _bookService.GetActiveBooksAsync();
                if (books == null)
                {
                    return View(new List<Book>());
                }
                return View(new HomeViewModel { Books = books });
            }
            //handle exceptions
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while retrieving book list. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

        // GET : Admin Dashboard View With Relevent Data
        [HttpGet]
        public async Task<IActionResult> AdminDashboard()
        {
            try
            {
                var model = await _homeService.GetDashboardDataAsync();
                return View(model);
            }
            //handle exceptions
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while retrieving admin dashboard data. Please try again later: {ex.Message}";
                return View("Error");
            }
        }

        // GET : Error View 
        [HttpGet]
        public IActionResult Error()
        {
            return View();
        }
    }
}
