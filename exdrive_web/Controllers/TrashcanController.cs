using exdrive_web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace exdrive_web.Controllers
{ 
    public class TrashcanController : Controller
    {
        private string? _userId;
        private static List<NameInstance> _nameInstances = new List<NameInstance>();
        private static List<NameInstance>? _deleted = new List<NameInstance>();
        private static bool _isDeleted = true;

        public IActionResult Trashcan()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _nameInstances = new List<NameInstance>(UserFilesDB.GetUserFilesDB(_userId));
            _deleted = null;
            return View(_deleted);
        }
        [HttpPost]
        public ActionResult Delete()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            List<NameInstance> deleted = new List<NameInstance>();
            int i = 0;
            foreach (var name in _nameInstances)
            {
                if (_isDeleted == true)
                {
                    deleted.Add(_nameInstances.ElementAt(i)); // add deleted files to list
                }
                else
                    _isDeleted = false;
                i++;
            }
            return View("AccessStorage", _deleted);
        }
    }
}
