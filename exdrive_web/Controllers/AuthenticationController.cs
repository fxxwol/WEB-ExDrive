using Microsoft.AspNetCore.Mvc;

namespace exdrive_web.Controllers
{
    public class AuthenticationController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
       
    }
}
