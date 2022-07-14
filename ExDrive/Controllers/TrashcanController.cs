using ExDrive.Models;
using ExDrive.Services;
using ExDrive.Authentication;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace ExDrive.Controllers
{
    public class TrashcanController : Controller
    {
        private string? _userId;
        private static bool _isDeleted = false;

        private static List<UserFile>? _deleted = new();
        private static List<UserFile>? _searchResult = new();

        private static ApplicationDbContext _applicationDbContext = new();

        private static readonly string _trashcanContainerName = "trashcan";

        public TrashcanController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }
        public IActionResult Trashcan()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var deletedFiles = new DeletedFiles();
            _deleted = new List<UserFile>(deletedFiles.GetDeletedFiles(_userId));

            _searchResult = null;
            return View(_deleted);
        }
        public ActionResult FileClick(string afile)
        {
            try
            {
                int position = Int32.Parse(afile);

                // if there was no search, file from main List is selected
                // (user is not in search mode)
                if (_searchResult == null)
                {
                    _deleted!.ElementAt(position).IsSelected ^= true;
                    return View("Trashcan", _deleted);
                }

                _searchResult.ElementAt(position).IsSelected ^= true;
                return View("Trashcan", _searchResult);
            }
            catch (Exception)
            {
                if (_searchResult == null)
                {
                    return View("Trashcan", _deleted);
                }

                return View("Trashcan", _searchResult);
            }
        }

        public ActionResult DeletePerm()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _isDeleted = true;

            if (_deleted == null)
            {
                return View("Trashcan", _deleted);
            }

            // if there was no search, files from main List are deleted
            // (user is not in search mode)
            if (_searchResult == null)
            {
                foreach (var name in _deleted)
                {
                    if (name.IsSelected == true)
                    {
                        var todelete = _applicationDbContext.Files!.Find(name.Id);
                        
                        if (todelete != null)
                        {
                            _applicationDbContext.Remove(todelete);

                            try
                            {
                                var deleteBlob = new DeleteAzureFile();

                                deleteBlob.DeleteBlobAsync(todelete, _trashcanContainerName);
                            }
                            catch
                            {

                            }
                        }
                    }
                }
                _applicationDbContext.SaveChanges();

                return RedirectToAction("Trashcan", "Trashcan");
            }

            // creating new list to preserve search results
            // function adds files that are not marked for deletion newsearch List
            int i = 0;
            List<UserFile> newsearch = new List<UserFile>();

            foreach (var name in _searchResult)
            {
                if (name.IsSelected == true)
                {
                    Files? todelete = _applicationDbContext.Files!.Find(name.Id);
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
        
        [Authorize]
        public IActionResult Search(string searchString)
        {
            ViewData["GetFiles"] = searchString;

            if (searchString == null && _searchResult != null)
            {
                return View("Trashcan", _searchResult);
            }

            if (searchString == null)
            {
                _searchResult = null;

                if (_isDeleted == false)
                {
                    return View("Trashcan", _deleted);
                }

                _isDeleted = false;
                return RedirectToAction("Trashcan", "Trashcan");
            }

            // checking if user files' names contain search request
            _searchResult = _deleted!.Where(x => x.Name.Contains(searchString)).ToList();
            if (_searchResult.Count > 0)
            {
                return View("Trashcan", _searchResult);
            }

            // if file wasn't deleted, returning old view
            if (_isDeleted == false)
            {
                return View("Trashcan", _deleted);
            }

            // if file is deleted, generating new List
            _isDeleted = false;
            return RedirectToAction("Trashcan", "Trashcan");
        }
      
        public async Task<ActionResult> Recovery()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (_searchResult == null && _deleted != null)
            {
                foreach (var name in _deleted)
                {
                    if (name.IsSelected == true)
                    {
                        var trashcan = new Trashcan(_trashcanContainerName);
                        await trashcan.RecoverFileAsync(name.Id, _userId, _applicationDbContext);
                    }
                }

                return RedirectToAction("Trashcan", "Trashcan");
            }

             var newSearch = new List<UserFile>();

            for (var elementPosition = 0; elementPosition < _searchResult!.Count; elementPosition++)
            {
                var name = _searchResult[elementPosition];

                if (name.IsSelected == true)
                {
                    var trashcan = new Trashcan(_trashcanContainerName);
                    await trashcan.RecoverFileAsync(name.Id, _userId, _applicationDbContext);
                }
                else
                {
                    newSearch.Add(_searchResult.ElementAt(elementPosition));
                }
            }

            _searchResult = newSearch;

            return View("Trashcan", _searchResult);
        }
    }
}
