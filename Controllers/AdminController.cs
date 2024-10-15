using Microsoft.AspNetCore.Mvc;

namespace EBookStore.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
