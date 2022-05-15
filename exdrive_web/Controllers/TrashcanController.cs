using exdrive_web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace exdrive_web.Controllers
{ 
    public class TrashcanController : Controller
    {
        private string? _userId;
        private static List<NameInstance>? _deleted = new List<NameInstance>();
        public IActionResult Trashcan()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _deleted = new List<NameInstance>(DeletedFiles.GetDeletedFilesDB(_userId));
            return View(_deleted);
        }
        //[HttpPost]
        //public ActionResult Deleted()
        //{
        //    _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    List<NameInstance> _deleted = new List<NameInstance>();
        //    int i = 0;
        //    foreach (var name in _nameInstances)
        //    {
        //        if (_isDeleted == true)
        //        {
        //            _deleted.Add(_nameInstances.ElementAt(i)); // add deleted files to list
        //        }
        //        else
        //            _isDeleted = false;
        //        i++;
        //    }
        //    return View("Trashcan", _deleted);
        //}
    }
}
