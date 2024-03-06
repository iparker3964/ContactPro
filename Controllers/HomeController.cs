using ContactPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ContactPro.Controllers
{
  
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        [Route("/Home/HandleError/{code:int}")]
        public IActionResult HandleError(int code)
        {
            string errorMessage = string.Empty;

            if (code == 404)
            {
                errorMessage = "The page you are looking for might have been removed had its name changed or is temporarily unavailable.";
            }
            else
            {
                errorMessage = "Sorry, something went wrong";
            }

            CustomError customError = new()
            {
                Code = code,
                Message = errorMessage
            };
            
        
            return View("~/Views/Shared/CustomError.cshtml",customError);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
