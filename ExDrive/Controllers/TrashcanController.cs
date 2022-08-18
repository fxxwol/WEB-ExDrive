using ExDrive.Models;
using ExDrive.Services;
using ExDrive.Authentication;
using ExDrive.Helpers.Constants;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace ExDrive.Controllers
{
    [Authorize]
    public class TrashcanController : Controller
    {
        public IActionResult Trashcan()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _deletedFiles = new List<UserFile>(new DeletedFiles().GetDeletedFiles(_userId));

            _searchResult = null;

            return View(_deletedFiles);
        }

        public ActionResult FileClickHandler(string afile)
        {
            var position = Int32.Parse(afile);

            if (_searchResult == null)
            {
                FileClick(position, _deletedFiles);

                return View("Trashcan", _deletedFiles);
            }
            else
            {
                FileClick(position, _searchResult);

                return View("Trashcan", _searchResult);
            }
        }

        public async Task<ActionResult> DeletePermanentlyHandler()
        {
            if (_searchResult == null)
            {
                await DeletePermanentlyWithoutPreservation(_deletedFiles);

                return RedirectToAction("Trashcan", "Trashcan");
            }
            else
            {
                await DeletePermanentlyWithPreservation(_searchResult);

                return View("Trashcan", _searchResult);
            }
        }

        public IActionResult SearchRedirectHandler(string searchString)
        {
            var result = Search(searchString);

            switch (result)
            {
                case SearchRedirect.ShowSearchResult:
                    return View("Trashcan", _searchResult);

                case SearchRedirect.ShowUserFiles:
                    return View("Trashcan", _deletedFiles);

                default:
                    return RedirectToAction("Trashcan", "Trashcan");
            }
        }

        public async Task<ActionResult> RecoverHandler()
        {
            if (_searchResult == null)
            {
                await RecoverWithoutPreservation(_deletedFiles);

                return RedirectToAction("Trashcan", "Trashcan");
            }
            else
            {
                await RecoverWithPreservation(_searchResult);

                return View("Trashcan", _searchResult);
            }
        }

        private SearchRedirect Search(string searchString)
        {
            ViewData["GetFiles"] = searchString;

            if (searchString == null && _searchResult != null)
            {
                return SearchRedirect.ShowSearchResult;
            }

            if (searchString == null)
            {
                if (_hasDeletionOccured == true)
                {
                    _hasDeletionOccured = false;

                    return SearchRedirect.UpdateUserFiles;
                }

                return SearchRedirect.ShowUserFiles;
            }

            PerformSearch(searchString);

            if (_searchResult!.Count > 0)
            {
                return SearchRedirect.ShowSearchResult;
            }

            if (_hasDeletionOccured == true)
            {
                _hasDeletionOccured = false;

                return SearchRedirect.UpdateUserFiles;
            }

            return SearchRedirect.ShowUserFiles;
        }

        private void PerformSearch(string searchString)
        {
            _searchResult = _deletedFiles.Where(file => file.Name.Contains(searchString)).ToList();
        }

        private async Task RecoverWithPreservation(List<UserFile> files)
        {
            var preservedResult = new List<UserFile>();

            for (var position = 0; position < files.Count; position++)
            {
                var file = files[position];

                if (file.IsSelected == true)
                {
                    await new Trashcan(ConstantValues._trashcanContainerName)
                        .RecoverFileAsync(file.Id, _userId, _applicationDbContext);
                }
                else
                {
                    preservedResult.Add(files.ElementAt(position));
                }
            }

            _searchResult = preservedResult;
        }

        private async Task RecoverWithoutPreservation(List<UserFile> files)
        {
            for (var position = 0; position < files.Count; position++)
            {
                var file = files[position];

                if (file.IsSelected == true)
                {
                    await new Trashcan(ConstantValues._trashcanContainerName)
                        .RecoverFileAsync(file.Id, _userId, _applicationDbContext);
                }
            }
        }

        private async Task DeletePermanentlyWithPreservation(List<UserFile> files)
        {
            _hasDeletionOccured = true;

            var preservedResult = new List<UserFile>();

            for (var position = 0; position < files.Count; position++)
            {
                var file = files[position];

                if (file.IsSelected == true)
                {
                    var fileToDelete = _applicationDbContext.Files!.Find(file.Id);

                    _deletedFiles.Remove(file);

                    _applicationDbContext.Remove(fileToDelete!);
                }
                else
                {
                    preservedResult.Add(files.ElementAt(position));
                }
            }

            _searchResult = preservedResult;

            await _applicationDbContext.SaveChangesAsync();
        }

        private async Task DeletePermanentlyWithoutPreservation(List<UserFile> files)
        {
            _hasDeletionOccured = true;

            for (var position = 0; position < files.Count; position++)
            {
                var file = files[position];

                if (file.IsSelected == true)
                {
                    var fileToDelete = _applicationDbContext.Files!.Find(file.Id);

                    _applicationDbContext.Remove(fileToDelete!);

                    await new DeleteAzureFile().DeleteBlobAsync(fileToDelete, ConstantValues._trashcanContainerName);
                }
            }

            await _applicationDbContext.SaveChangesAsync();
        }

        private void FileClick(int position, List<UserFile> files)
        {
            files.ElementAt(position).IsSelected ^= true;
        }

        public TrashcanController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        private static bool _hasDeletionOccured = false;

        private static List<UserFile> _deletedFiles = new();
        private static List<UserFile>? _searchResult = new();

        private static string _userId = String.Empty;
        private static ApplicationDbContext _applicationDbContext = new();
    }
}
