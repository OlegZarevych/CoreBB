using Microsoft.AspNetCore.Mvc;

namespace CoreBB.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}