using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ExDrive.Helpers;
using ExDrive.Models;
using ExDrive.Authentication;
using ExDrive.Services;

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

                default:
                    return RedirectToAction("AccessStorage", "Storage");
            }
        }

        [Authorize]
        public IActionResult ViewFavourite()
        {
            _isUserViewingFavourite = true;

            if (_searchResult != null)
            {
                return View("AccessStorage", _userFiles);
            }

            FilterFavourite();

            return View("AccessStorage", _searchResult);
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
        [RequestFormLimits(MultipartBodyLengthLimit = 629145600)]
        public async Task<IActionResult> SingleTempFileHandler(UploadInstance receivedFile)
        {
            if (receivedFile.MyFile == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var uploadHandler = new UploadTempFile();

            try
            {
                TempData["AlertMessage"] = await SingleTempFile(receivedFile, uploadHandler);
            }
            catch
            {
                TempData["AlertMessage"] = "Failed to upload: file may contain viruses";
            }

            await uploadHandler.DisposeAsync();

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = 629145600)]
        public async Task<IActionResult> SinglePermFileHandler(UploadInstance receivedFile)
        {
            if (String.IsNullOrEmpty(_userId) || receivedFile.MyFile == null)
            {
                return RedirectToAction("AccessStorage", "Storage");
            }

            var uploadHandler = new UploadPermFile();

            await SinglePermFile(receivedFile, uploadHandler);

            await uploadHandler.DisposeAsync();

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
            string path = Path.Combine(_tmpFilesPath, _userId);

            if (_searchResult == null)
            {
                await new DownloadFiles().DownloadFilesToFolderAsync(_userFiles, path, _userId);
            }
            else
            {
                await new DownloadFiles().DownloadFilesToFolderAsync(_searchResult, path, _userId);
            }

            var files = Directory.GetFiles(path).ToList();

            if (files.Count < 1 && _searchResult == null)
            {
                return View("AccessStorage", _userFiles);
            }

            if (files.Count < 1)
            {
                return View("AccessStorage", _searchResult);
            }

            using (var result = new CreateArchive().Create(files, path))
            {
                Directory.Delete(path, true);

                return File(result.ToArray(), "application/zip",
                            $"archive-{DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss")}.zip");
            }
        }
        
        [Authorize]
        public async Task<string?> GetLinkHandler()
        {
            string path = Path.Combine(_getLinkArchive, _userId);

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

            var zipName = $"archive-{DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss")}.zip";

            using var result = new CreateArchive().Create(files, path);

            Directory.Delete(path, true);

            var uploadHandler = new UploadTempFile();

            try
            {
                return await SingleTempFile(result, zipName, uploadHandler);
            }
            catch
            {
                return "File may be malicious";
            }
            finally
            {
                await uploadHandler.DisposeAsync();
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

            var readFileContext = new ReadFileContext();

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

            using (var stream = new DownloadAzureFile().DownloadFileAsync(file.Id, _userId).Result)
            {
                await stream.CopyToAsync(memoryStream);
            }

            await readFileContext.ExecuteStrategy(HttpContext, memoryStream);

            readFileContext.Dispose();
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
                if (String.IsNullOrWhiteSpace(newName))
                {
                    await Rename(_searchResult, newName);

                    DeselectAll();
                }

                return View("AccessStorage", _searchResult);
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

            var toRename = _applicationDbContext.Files!.Find(file.Id);

            var renamed = toRename;

            renamed!.Name = new GetValidName().GetName(newName, toRename!.Name);

            _applicationDbContext.Files.Update(toRename).OriginalValues.SetValues(renamed);

            files.Find(entry => entry.Id == toRename.FilesId)!.Name = renamed.Name;

            await _applicationDbContext.SaveChangesAsync();

        }

        // Needs refactoring
        [HttpPost, DisableRequestSizeLimit]
        public async Task<ActionResult> UploadTempFileBot()
        {
            if (Request.ContentLength == 0 || Request.ContentLength == null)
            {
                HttpResponse badresponse = Response;
                badresponse.Clear();
                badresponse.StatusCode = 500;

                return BadRequest(badresponse);
            }

            Microsoft.Extensions.Primitives.StringValues vs;
            Request.Headers.TryGetValue("file-name", out vs);
            string name = vs.First();

            var fileFormat = new FindFileFormat();
            var file = new Files(Guid.NewGuid().ToString() + fileFormat.FindFormat(name),
                name, "*", true);

            Stream stream = Request.Body;

            try
            {
                var uploadTempBotFile = new UploadTempBotFile();

                await uploadTempBotFile.UploadFileAsync(stream, (long)Request.ContentLength, Guid.NewGuid().ToString(),
                                            file, _applicationDbContext);
            }
            catch (Exception)
            {
                HttpResponse badresponse = Response;
                badresponse.Clear();
                badresponse.StatusCode = 500;

                return BadRequest(badresponse);
            }

            HttpResponse response = Response;
            response.Clear();
            response.StatusCode = 200;
            response.ContentType = "text/xml";
            await response.WriteAsync(_tempFilesContainerLink + file.FilesId);

            return Ok(response);
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

                modified.Favourite ^= true;

                _applicationDbContext.Files.Update(toModify).OriginalValues.SetValues(modified);

                await _applicationDbContext.SaveChangesAsync();
            }
        }

        private async Task<string> SingleTempFile(UploadInstance receivedFile, UploadTempFile uploadHandler)
        {
            var newName = new GetUniqueName().GetName(receivedFile.MyFile!.FileName);

            var file = new Files(newName, receivedFile.MyFile.FileName);

            await uploadHandler.UploadFileAsync(receivedFile, file, Guid.NewGuid().ToString(), _applicationDbContext);

            return _tempFilesContainerLink + file.FilesId;
        }

        private async Task<string> SingleTempFile(MemoryStream memoryStream, string fileName, UploadTempFile uploadHandler)
        {
            var newName = new GetUniqueName().GetName(fileName);

            var file = new Files(newName, fileName);

            await uploadHandler.UploadFileAsync(memoryStream, file, _applicationDbContext);

            return _tempFilesContainerLink + file.FilesId;
        }

        private async Task SinglePermFile(UploadInstance receivedFile, UploadPermFile uploadHandler)
        {
            var newName = new GetUniqueName().GetName(receivedFile.MyFile!.FileName);

            var file = new Files(newName, receivedFile.MyFile.FileName, _userId!, false);

            await uploadHandler.UploadFileAsync(receivedFile, file, _userId!, _applicationDbContext);
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
                    await new Trashcan(_trashcanContainerName).DeleteFileAsync(file.Id, _userId, _applicationDbContext);
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
                    await new Trashcan(_trashcanContainerName).DeleteFileAsync(file.Id, _userId, _applicationDbContext);
                }
            }
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
            files.ElementAt(position).IsSelected ^= true;
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

        private static readonly string _tmpFilesPath = "C:\\Users\\Public\\tmpfiles\\";
        private static readonly string _getLinkArchive = "C:\\Users\\Public\\getlink\\";
        private static readonly string _tempFilesContainerLink = "https://exdrivefile.blob.core.windows.net/botfiles/";
        private static readonly string _trashcanContainerName = "trashcan";
    }
}