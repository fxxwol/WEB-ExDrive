using exdrive_web.Models;
using JWTAuthentication.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace exdrive_web.Controllers
{
    public class TrashcanController : Controller
    {
        private string? _userId;
        private static List<NameInstance>? _deleted = new List<NameInstance>();
        private static List<NameInstance>? _searchResult = new List<NameInstance>();
        public IActionResult Trashcan()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _deleted = new List<NameInstance>(DeletedFiles.GetDeletedFilesDB(_userId));
            _searchResult = null;
            return View(_deleted);
        }
        public ActionResult FileClick(string afile)
        {
            int position = Int32.Parse(afile);

            // if there was no search, file from main List is selected
            // (user is not in search mode)
            if (_searchResult == null)
            {
                if (_deleted != null)
                {
                    _deleted.ElementAt(position).IsSelected ^= true;
                    return View("Trashcan", _deleted);
                }
            }
            else
                _searchResult.ElementAt(position).IsSelected ^= true;

            return View("Trashcan", _searchResult);
        }
        [HttpPost]
        public ActionResult DeletePerm()
        {
            //int i = 0;
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(ExFunctions.sqlConnectionString);
            using (var _context = new ApplicationDbContext(optionsBuilder.Options))
            {
                if (_deleted != null)
                {
                    foreach (var name in _deleted)
                    {
                        if (name.IsSelected == true)
                        {
                            Files? todelete = _context.Files.Find(name.Id);
                            if (todelete != null)
                            {
                                _deleted.Remove(name);
                                _context.Remove(todelete);
                            }
                        }
                        _context.SaveChanges();
                        break;
                        //i++;
                    }
                }

            }

            return View("Trashcan", _deleted);
        }
    }
}
