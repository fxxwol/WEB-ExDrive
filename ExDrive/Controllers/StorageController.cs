using ExDrive.Authentication;
using ExDrive.Helpers;
using ExDrive.Helpers.Constants;
using ExDrive.Models;
using ExDrive.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

using System.Security.Claims;

namespace ExDrive.Controllers
{
    public class StorageController : Controller
    {
        [Authorize]
        public async Task<IActionResult> AccessStorage()
        {
            _isUserViewingFavourite = false;

            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _userFiles = await new UserFilesDB().GetUserFilesDBAsync(_userId);

            _searchResult = null;

            return View(_userFiles);
        }

        [Authorize]
        public IActionResult Trashcan()
        {
            return View();
        }

        [Authorize]
        public IActionResult SearchRedirectHandler(string searchString)
        {
            var result = Search(searchString);

            switch (result)
            {
                case SearchRedirect.ShowSearchResult:
                    return View("AccessStorage", _searchResult);

                case SearchRedirect.ShowUserFiles:
                    return View("AccessStorage", _userFiles);

                case SearchRedirect.UpdateUserFiles:
                    return RedirectToAction("AccessStorage", "Storage");

                default:
                    return RedirectToAction("AccessStorage", "Storage");
            }
        }

        [Authorize]
        public IActionResult ViewFavourite()
        {
            _isUserViewingFavourite = true;

            if (_searchResult == null)
            {
                FilterFavourite();

                return View("AccessStorage", _searchResult);
            }

            return View("AccessStorage", _userFiles);
        }

        public IActionResult SingleFile()
        {
            return View(new UploadInstance());
        }

        [Authorize]
        public async Task<ActionResult> FavouriteHandler()
        {
            if (_searchResult == null)
            {
                await Favourite(_userFiles);
            }
            else
            {
                await Favourite(_searchResult);
            }

            return RedirectToAction("AccessStorage", "Storage");
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SingleTempFileHandler(UploadInstance receivedFile)
        {
            if (receivedFile.MyFile == null)
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                TempData["AlertMessage"] = await SingleTempFile(receivedFile);
            }
            catch
            {
                TempData["AlertMessage"] = "Failed to upload: file may contain viruses";
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SinglePermFileHandler(UploadInstance receivedFile)
        {
            if (receivedFile.MyFile != null)
            {
                await SinglePermFile(receivedFile);
            }

            return RedirectToAction("AccessStorage", "Storage");
        }

        [Authorize]
        public ActionResult FileClickHandler(string afile)
        {
            var position = Int32.Parse(afile);

            if (_searchResult == null)
            {
                FileClick(position, _userFiles);

                return View("AccessStorage", _userFiles);
            }
            else
            {
                FileClick(position, _searchResult);

                return View("AccessStorage", _searchResult);
            }
        }


        [Authorize]
        public async Task<IActionResult> DeleteHandler()
        {
            if (_searchResult == null)
            {
                await DeleteWithoutPreservation(_userFiles);

                return RedirectToAction("AccessStorage", "Storage");
            }
            else
            {
                await DeleteWithPreservation(_searchResult);

                return View("AccessStorage", _searchResult);
            }
        }

        [Authorize]
        public async Task<ActionResult> DownloadFilesHandler()
        {
            string path = Path.Combine(ConstantValues._temporaryFilesFolderPath, _userId);

            if (_searchResult == null)
            {
                await new DownloadFiles().DownloadFilesToFolderAsync(_userFiles, path, _userId);
            }
            else
            {
                await new DownloadFiles().DownloadFilesToFolderAsync(_searchResult, path, _userId);
            }

            var files = Directory.GetFiles(path).ToList();

            if (files.Count < 1)
            {
                if (_searchResult == null)
                {
                    return View("AccessStorage", _userFiles);
                }
                else
                {
                    return View("AccessStorage", _searchResult);
                }
            }

            using var result = new CreateArchive().Create(files, path);

            var archiveName = $"archive-{DateTime.Now:yyyy_MM_dd-HH_mm_ss}.zip";

            Directory.Delete(path, true);

            return File(result.ToArray(), "application/zip", archiveName);
        }

        [Authorize]
        public async Task<string?> GetLinkHandler()
        {
            string path = Path.Combine(ConstantValues._temporaryArchiveFolderPath, _userId);

            if (_searchResult == null)
            {
                await new DownloadFiles().DownloadFilesToFolderAsync(_userFiles, path, _userId);
            }
            else
            {
                await new DownloadFiles().DownloadFilesToFolderAsync(_searchResult, path, _userId);
            }

            var files = Directory.GetFiles(path).ToList();

            if (files.Count < 1)
            {
                return null;
            }

            var archiveName = $"archive-{DateTime.Now:yyyy_MM_dd-HH_mm_ss}.zip";

            using var result = new CreateArchive().Create(files, path);

            Directory.Delete(path, true);

            try
            {
                return await SingleTempFile(result, archiveName);
            }
            catch
            {
                return "File may be malicious";
            }
        }

        [HttpPost]
        [Authorize]
        public async void ReadFileHandler()
        {
            var file = FindFirstSelectedFile();

            if (file == null)
            {
                return;
            }

            using var readFileContext = new ReadFileContext();

            switch (new FindFileFormat().FindFormat(file.Name))
            {
                case ".jpg":
                case ".jpeg":
                    readFileContext.SetStrategy(new ReadFileJpegConcreteStrategy());
                    break;

                case ".png":
                    readFileContext.SetStrategy(new ReadFilePngConcreteStrategy());
                    break;

                case ".pdf":
                    readFileContext.SetStrategy(new ReadFilePdfConcreteStrategy());
                    break;

                case ".docx":
                case ".doc":
                    readFileContext.SetStrategy(new ReadFileDocxConcreteStrategy());
                    break;

                case ".txt":
                    readFileContext.SetStrategy(new ReadFileTxtConcreteStrategy());
                    break;

                default:
                    readFileContext.SetStrategy(new ReadFileDefaultConcreteStrategy());
                    break;
            }

            using var memoryStream = new MemoryStream();

            using var downloadedFileStream = new DownloadAzureFile().DownloadFileAsync(file.Id, _userId).Result;
                
            await downloadedFileStream.CopyToAsync(memoryStream);

            await readFileContext.ExecuteStrategy(HttpContext, memoryStream);
        }

        [Authorize]
        public async Task<ActionResult> RenameHandler(string newName)
        {
            if (_searchResult == null)
            {
                if (!String.IsNullOrWhiteSpace(newName))
                {
                    await Rename(_userFiles, newName);

                    DeselectAll();
                }

                return View("AccessStorage", _userFiles);
            }
            else
            {
                if (!String.IsNullOrWhiteSpace(newName))
                {
                    await Rename(_searchResult, newName);

                    DeselectAll();
                }

                return View("AccessStorage", _searchResult);
            }
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<ActionResult> SingleTempBotFileHandler()
        {
            if (Request.ContentLength == 0 || Request.ContentLength == null)
            {
                return BadRequest();
            }

            try
            {
                var uploadedFile = await SingleTempBotFile(Request);

                return Ok(new BotSuccessResponse().Write(Response,
                        ConstantValues._temporaryFilesContainerLink + uploadedFile.FilesId));
            }
            catch
            {
                return BadRequest();
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

            if (_isUserViewingFavourite == true)
            {
                FilterFavourite();

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
            if (_isUserViewingFavourite == true)
            {
                _searchResult = _userFiles.Where(file => file.Name.Contains(searchString) && file.IsFavourite == true).ToList();
            }
            else
            {
                _searchResult = _userFiles.Where(file => file.Name.Contains(searchString)).ToList();
            }
        }

        private async Task Favourite(List<UserFile> files)
        {
            foreach (var file in files)
            {
                if (file.IsSelected == false)
                    continue;

                var toModify = await _applicationDbContext.Files!.FindAsync(file.Id);

                if (toModify == null)
                    continue;

                var modified = toModify;

                modified.Favourite = !modified.Favourite;

                _applicationDbContext.Files.Update(toModify).OriginalValues.SetValues(modified);

                await _applicationDbContext.SaveChangesAsync();
            }
        }

        private async Task<string> SingleTempFile(UploadInstance receivedFile)
        {
            var newName = new GetUniqueName().GetName(receivedFile.MyFile!.FileName);

            var file = new Files(newName, receivedFile.MyFile.FileName);

            using var uploadHandler = new UploadTempFile();

            await uploadHandler.UploadFileAsync(receivedFile, file, Guid.NewGuid().ToString(), _applicationDbContext);

            return ConstantValues._temporaryFilesContainerLink + file.FilesId;
        }

        private async Task<string> SingleTempFile(MemoryStream memoryStream, string fileName)
        {
            var newName = new GetUniqueName().GetName(fileName);

            var file = new Files(newName, fileName);

            using var uploadHandler = new UploadTempFile();

            await uploadHandler.UploadFileAsync(memoryStream, file, _applicationDbContext);

            return ConstantValues._temporaryFilesContainerLink + file.FilesId;
        }

        private async Task SinglePermFile(UploadInstance receivedFile)
        {
            var newName = new GetUniqueName().GetName(receivedFile.MyFile!.FileName);

            var file = new Files(newName, receivedFile.MyFile.FileName, _userId!, false);

            using var uploadHandler = new UploadPermFile();

            await uploadHandler.UploadFileAsync(receivedFile, file, _userId!, _applicationDbContext);
        }

        private async Task<Files> SingleTempBotFile(HttpRequest httpRequest)
        {
            httpRequest.Headers.TryGetValue("file-name", out StringValues fileName);

            string oldName = fileName.First();

            var file = new Files(new GetUniqueName().GetName(oldName), oldName);

            var fileStream = httpRequest.Body;

            await new UploadTempBotFile().UploadFileAsync(fileStream, (long)httpRequest.ContentLength!,
                                                                file, _applicationDbContext);

            return file;
        }

        private async Task DeleteWithPreservation(List<UserFile> files)
        {
            _hasDeletionOccured = true;

            var preservedResult = new List<UserFile>();

            for (var position = 0; position < files.Count; position++)
            {
                var file = files[position];

                if (file.IsSelected == true)
                {
                    await new Trashcan(ConstantValues._trashcanContainerName)
                        .DeleteFileAsync(file.Id, _userId, _applicationDbContext);
                }
                else
                {
                    preservedResult.Add(files.ElementAt(position));
                }
            }

            _searchResult = preservedResult;
        }

        private async Task DeleteWithoutPreservation(List<UserFile> files)
        {
            _hasDeletionOccured = true;

            for (var position = 0; position < files.Count; position++)
            {
                var file = files[position];

                if (file.IsSelected == true)
                {
                    await new Trashcan(ConstantValues._trashcanContainerName)
                        .DeleteFileAsync(file.Id, _userId, _applicationDbContext);
                }
            }
        }

        private async Task Rename(List<UserFile> files, string newName)
        {
            ViewData["Rename"] = newName;

            var file = FindFirstSelectedFile();

            if (file == null)
            {
                return;
            }

            var fileToRename = _applicationDbContext.Files!.Find(file.Id);

            var renamedFile = fileToRename;

            renamedFile!.Name = new GetValidName().GetName(newName, fileToRename!.Name);

            _applicationDbContext.Files.Update(fileToRename).OriginalValues.SetValues(renamedFile);
            
            RenameElementsWithId(files, fileToRename.FilesId, renamedFile.Name);

            await _applicationDbContext.SaveChangesAsync();
        }

        private void RenameElementsWithId(List<UserFile> files, string fileIdToMatch, string newName)
        {
            files.Find(entry => entry.Id == fileIdToMatch)!.Name = newName;
        }

        private UserFile? FindFirstSelectedFile()
        {
            if (_searchResult == null)
            {
                return _userFiles.Find(file => file.IsSelected == true);
            }
            else
            {
                return _searchResult.Find(file => file.IsSelected == true);
            }
        }

        private void DeselectAll()
        {
            if (_searchResult == null)
            {
                _userFiles.ForEach(file => file.IsSelected = false);
            }
            else
            {
                _searchResult.ForEach(file => file.IsSelected = false);
            }
        }

        private void FileClick(int position, List<UserFile> files)
        {
            files.ElementAt(position).IsSelected = !files.ElementAt(position).IsSelected;
        }

        private void FilterFavourite()
        {
            _searchResult = _userFiles.Where(file => file.IsFavourite == true).ToList();
        }

        public StorageController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        private static List<UserFile> _userFiles = new();
        private static List<UserFile>? _searchResult = new();

        private static ApplicationDbContext _applicationDbContext = new();

        private static string _userId = String.Empty;
        private static bool _hasDeletionOccured = false;
        private static bool _isUserViewingFavourite = false;
    }
}