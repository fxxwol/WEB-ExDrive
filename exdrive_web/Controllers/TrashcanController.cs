using exdrive_web.Models;
using JWTAuthentication.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace exdrive_web.Controllers
{
    public class TrashcanController : Controller
    {
        private string? _userId;
        private static bool _isDeleted = false;

        private static List<NameInstance>? _deleted = new List<NameInstance>();
        private static List<NameInstance>? _searchResult = new List<NameInstance>();

        private static ApplicationDbContext _applicationDbContext;

        public TrashcanController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }
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
            {
                _searchResult.ElementAt(position).IsSelected ^= true;
            }

            return View("Trashcan", _searchResult);
        }

        public ActionResult DeletePerm()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _isDeleted = true;

            if (_deleted == null)
                return View("Trashcan", _deleted);

            // if there was no search, files from main List are deleted
            // (user is not in search mode)
            if (_searchResult == null)
            {
                foreach (var name in _deleted)
                {
                    if (name.IsSelected == true)
                    {
                        Files? todelete = _applicationDbContext.Files.Find(name.Id);
                        if (todelete != null)
                        {
                            _applicationDbContext.Remove(todelete);
                        }
                    }
                }
                _applicationDbContext.SaveChanges();

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
                    Files? todelete = _applicationDbContext.Files.Find(name.Id);
                    if (todelete != null)
                    {
                        _deleted.Remove(name);

                        _applicationDbContext.Remove(todelete);

                    }
                }
                else
                {
                    newsearch.Add(_searchResult.ElementAt(i));
                }

                i++;
            }

            _applicationDbContext.SaveChanges();

            _searchResult = newsearch;
            return View("Trashcan", _searchResult);
        }

        //[Authorize]
        //public IActionResult Search(string searchString)
        //{
        //    ViewData["GetFiles"] = searchString;

        //    if (searchString == null)
        //    {
        //        _searchResult = null;

        //        if (_isDeleted == false)
        //            return View("Trashcan", _deleted);

        //        _isDeleted = false;
        //        return RedirectToAction("Trashcan", "Trashcan");
        //    }

        //    // checking a file's name without format first
        //    _searchResult = _deleted.Where(x => x.NoFormat.Equals(searchString)).ToList();
        //    if (_searchResult.Count > 0)
        //        return View("Trashcan", _searchResult);

        //    // checking a file's name with format
        //    _searchResult = _deleted.Where(x => x.Name.Equals(searchString)).ToList();
        //    if (_searchResult.Count > 0)
        //        return View("Trashcan", _searchResult);

        //    // if file wasn't deleted, returning old view
        //    if (_isDeleted == false)
        //        return View("Trashcan", _deleted);

        //    // if file is deleted, generating new List
        //    _isDeleted = false;
        //    return RedirectToAction("Trashcan", "Trashcan");
        //}
        //public ActionResult DeletePerm()
        //{
        //    if (_deleted != null)
        //    {
        //        foreach (var name in _deleted)
        //        {
        //            if (name.IsSelected == true)
        //            {
        //                Files? todelete = _applicationDbContext.Files.Find(name.Id);
        //                if (todelete != null)
        //                {
        //                    _deleted.Remove(name);

        //                    _applicationDbContext.Remove(todelete);

        //                    _applicationDbContext.SaveChanges();
        //                }
        //            }

        //        }
        //    }

        //    return View("Trashcan", _deleted);
        //}
        [Authorize]
        public IActionResult Search(string searchString)
        {
            ViewData["GetFiles"] = searchString;

            if (searchString == null)
            {
                _searchResult = null;

                if (_isDeleted == false)
                    return View("Trashcan", _deleted);

                _isDeleted = false;
                return RedirectToAction("Trashcan", "Trashcan");
            }

            // checking if user files' names contain search request
            _searchResult = _deleted.Where(x => x.Name.Contains(searchString)).ToList();
            if (_searchResult.Count > 0)
                return View("Trashcan", _searchResult);

            // if file wasn't deleted, returning old view
            if (_isDeleted == false)
                return View("Trashcan", _deleted);

            // if file is deleted, generating new List
            _isDeleted = false;
            return RedirectToAction("Trashcan", "Trashcan");
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
                {
                    newsearch.Add(_searchResult.ElementAt(i));
                }

                i++;
            }

            _searchResult = newsearch;

            return View("Trashcan", _searchResult);
        }
    }
}
