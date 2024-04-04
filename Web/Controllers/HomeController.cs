using System.Diagnostics;
using CodeConverterWebApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace CodeConverterWebAppCore.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }
    }
} 