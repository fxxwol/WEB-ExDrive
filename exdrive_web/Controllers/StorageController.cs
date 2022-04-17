using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace exdrive_web.Controllers
{
    [Authorize]
    public class StorageController : Controller
    {
        public IActionResult AccessStorage()
        {
            return View();
        }
    }
}
