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
                    }
                }

            }
            return View("Trashcan", _deleted);
        }
        [HttpPost]
        public async Task<ActionResult> Recovery()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // if there was no search, files from main List are deleted
            // (user is not in search mode)

            if (_searchResult == null && _deleted != null)
            {
                foreach (var name in _deleted)
                {
                    if (name.IsSelected == true)
                    {
                        await exdrive_web.Models.Trashcan.FileRecovery(name.Id, _userId);
                    }
                  
                }
                return RedirectToAction("Trashcan", "Trashcan");
            }
            // creating new list to preserve search results
            // function adds files that are not marked for deletion newsearch List
            int i = 0;
            List<NameInstance> newsearch = new List<NameInstance>();

            foreach (var name in _searchResult)
            {
                if (name.IsSelected == true)
                {
                    await exdrive_web.Models.Trashcan.FileRecovery(name.Id, _userId);
                }
                else
                    newsearch.Add(_searchResult.ElementAt(i));

                i++;
            }

            _searchResult = newsearch;
            return View("Trashcan", _searchResult);
        }
    }
}
