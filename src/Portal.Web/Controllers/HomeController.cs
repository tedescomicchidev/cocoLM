using Microsoft.AspNetCore.Mvc;

namespace Portal.Web.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
