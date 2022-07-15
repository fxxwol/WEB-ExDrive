using System.IO.Compression;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;

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

        // path = tempPath + _userId
        private async Task DownloadFilesToFolder(List<UserFile> files, string path)
        {
            Directory.CreateDirectory(path);

            for (var position = 0; position < files.Count; position++)
            {
                var file = files[position];

                if (file.IsSelected == false)
                    continue;

                await DownloadFileToFolder(file.Id, GetFileName(file, path, position), path);
            }
        }

        private async Task DownloadFileToFolder(string fileId, string fileName, string path)
        {
            using (var fileStream = new FileStream(Path.Combine(path, fileName),
                                                          FileMode.Create, FileAccess.Write))
            {
                var downloadFile = new DownloadAzureFile();

                var memoryStream = await downloadFile.DownloadFileAsync(fileId, _userId);

                memoryStream.Position = 0;

                await memoryStream.CopyToAsync(fileStream);

                await memoryStream.FlushAsync();
            }
        }

        private string GetFileName(UserFile file, string path, int position)
        {
            var downloadedFiles = Directory.GetFiles(path).ToList();

            var currentName = Path.Combine(path, file.Name);

            if (downloadedFiles.Contains(currentName))
            {
                return file.NoFormat + $"({position})" +
                                new FindFileFormat().FindFormat(file.Name);
            }
            else
            {
                return file.Name;
            }
        }


        [Authorize]
        public async Task<ActionResult> DownloadFiles()
        {

            string path = Path.Combine(_tmpFilesPath, _userId);

            if (_searchResult == null)
            {
                await DownloadFilesToFolder(_userFiles, path);
            }
            else
            {
                await DownloadFilesToFolder(_searchResult, path);
            }

            // Needs refactoring
            //if (_searchResult == null)
            //{
            //    int i = -1;
            //    foreach (var name in _userFiles)
            //    {
            //        i++;

            //        if (name.IsSelected == false)
            //            continue;

            //        var downloadedFiles = Directory.GetFiles(Path.Combine(_tmpFilesPath, _userId)).ToList();

            //        var fileName = String.Empty;

            //        var currentName = Path.Combine(_tmpFilesPath + _userId, _userFiles.ElementAt(i).Name);

            //        if (downloadedFiles.Contains(currentName))
            //        {
            //            var findFormat = new FindFileFormat();

            //            fileName = _userFiles.ElementAt(i).NoFormat + $"({i})" +
            //                findFormat.FindFormat(_userFiles.ElementAt(i).Name);
            //        }
            //        else
            //        {
            //            fileName = _userFiles.ElementAt(i).Name;
            //        }

            //        using (var fileStream = new FileStream(Path.Combine(_tmpFilesPath + _userId,
            //                                                            fileName),
            //                                                            FileMode.Create, FileAccess.Write))
            //        {
            //            var downloadFile = new DownloadAzureFile();

            //            var memoryStream = await downloadFile.DownloadFileAsync(_userFiles.ElementAt(i).Id, _userId);

            //            memoryStream.Position = 0;

            //            await memoryStream.CopyToAsync(fileStream);

            //            await memoryStream.FlushAsync();
            //        }
            //    }
            //}
            //else
            //{
            //    int i = -1;
            //    foreach (var name in _searchResult)
            //    {
            //        i++;

            //        if (name.IsSelected == false)
            //            continue;

            //        var downloadedFiles = Directory.GetFiles(Path.Combine(_tmpFilesPath, _userId)).ToList();

            //        var fileName = String.Empty;

            //        if (downloadedFiles.Contains(Path.Combine(_tmpFilesPath + _userId, _searchResult.ElementAt(i).Name)))
            //        {
            //            var findFormat = new FindFileFormat();

            //            fileName = _searchResult.ElementAt(i).NoFormat + $"({i})" +
            //                findFormat.FindFormat(_searchResult.ElementAt(i).Name);
            //        }
            //        else
            //        {
            //            fileName = _searchResult.ElementAt(i).Name;
            //        }

            //        using (var fileStream = new FileStream(Path.Combine(_tmpFilesPath + _userId,
            //                                                            fileName),
            //                                                            FileMode.Create, FileAccess.Write))
            //        {
            //            var downloadFile = new DownloadAzureFile();

            //            var memoryStream = await downloadFile.DownloadFileAsync(_searchResult.ElementAt(i).Id, _userId);

            //            memoryStream.Position = 0;

            //            await memoryStream.CopyToAsync(fileStream);

            //            await memoryStream.FlushAsync();
            //        }
            //    }
            //}
            //

            var files = Directory.GetFiles(path).ToList();

            if (files.Count < 1 && _searchResult == null)
            {
                return View("AccessStorage", _userFiles);
            }

            if (files.Count < 1)
            {
                return View("AccessStorage", _searchResult);
            }

            var zipName = $"archive-{DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss")}.zip";

            // Needs refactoring
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    files.ForEach(file =>
                    {
                        archive.CreateEntryFromFile(file, file.Replace(Path.Combine(_tmpFilesPath + _userId)
                                                                       + "\\", string.Empty));
                    });
                }

                Directory.Delete(Path.Combine(_tmpFilesPath, _userId), true);

                return File(memoryStream.ToArray(), "application/zip", zipName);
            }
            //
        }

        [Authorize]
        public async Task<string?> GetLink()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var zipName = $"archive-{DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss")}.zip";

            Directory.CreateDirectory(Path.Combine(_getLinkArchive, _userId));

            // Needs refactoring
            if (_searchResult == null)
            {
                int i = -1;
                foreach (var name in _userFiles)
                {
                    i++;

                    if (name.IsSelected == false)
                        continue;

                    var downloadedFiles = Directory.GetFiles(Path.Combine(_getLinkArchive, _userId)).ToList();

                    var fileName = String.Empty;

                    var currentName = Path.Combine(_getLinkArchive + _userId, _userFiles.ElementAt(i).Name);

                    if (downloadedFiles.Contains(currentName))
                    {
                        var findFormat = new FindFileFormat();

                        fileName = _userFiles.ElementAt(i).NoFormat + $"({i})" +
                            findFormat.FindFormat(_userFiles.ElementAt(i).Name);
                    }
                    else
                    {
                        fileName = _userFiles.ElementAt(i).Name;
                    }

                    using (var fileStream = new FileStream(Path.Combine(_getLinkArchive + _userId,
                                                                        fileName),
                                                                        FileMode.Create, FileAccess.Write))
                    {
                        var downloadFile = new DownloadAzureFile();

                        var memoryStream = await downloadFile.DownloadFileAsync(_userFiles.ElementAt(i).Id, _userId);

                        memoryStream.Position = 0;

                        await memoryStream.CopyToAsync(fileStream);

                        await memoryStream.FlushAsync();
                    }
                }
            }
            else
            {
                int i = -1;
                foreach (var name in _searchResult)
                {
                    i++;

                    if (name.IsSelected == false)
                        continue;

                    var downloadedFiles = Directory.GetFiles(Path.Combine(_getLinkArchive, _userId)).ToList();

                    var fileName = String.Empty;

                    var currentName = Path.Combine(_getLinkArchive + _userId, _searchResult.ElementAt(i).Name);

                    if (downloadedFiles.Contains(currentName))
                    {
                        var findFormat = new FindFileFormat();

                        fileName = _searchResult.ElementAt(i).NoFormat + $"({i})" +
                            findFormat.FindFormat(_searchResult.ElementAt(i).Name);
                    }
                    else
                    {
                        fileName = _searchResult.ElementAt(i).Name;
                    }

                    using (var fileStream = new FileStream(Path.Combine(_getLinkArchive + _userId,
                                                                        fileName),
                                                                        FileMode.Create, FileAccess.Write))
                    {
                        var downloadFile = new DownloadAzureFile();

                        var memoryStream = await downloadFile.DownloadFileAsync(_searchResult.ElementAt(i).Id, _userId);

                        memoryStream.Position = 0;

                        await memoryStream.CopyToAsync(fileStream);

                        await memoryStream.FlushAsync();
                    }
                }
            }
            //

            var files = Directory.GetFiles(Path.Combine(_getLinkArchive, _userId)).ToList();

            if (files.Count < 1)
            {
                return null;
            }

            // Needs refactoring
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    files.ForEach(file =>
                    {
                        archive.CreateEntryFromFile(file, file.Replace(Path.Combine(_getLinkArchive + _userId)
                                                                       + "\\", string.Empty));
                    });
                }

                var file = new Files(Guid.NewGuid().ToString() + ".zip", _userId + zipName, "*", true);

                var uploadTempAsync = new UploadTempFile();

                await uploadTempAsync.UploadFileAsync(memoryStream, file, _applicationDbContext);

                await uploadTempAsync.DisposeAsync();

                Directory.Delete(Path.Combine(_getLinkArchive, _userId), true);

                return _tempFilesContainerLink + file.FilesId;
            }
            //
        }

        [HttpPost]
        [Authorize]
        public async void ReadFile()
        {
            Stream stream;
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Needs refactoring
            foreach (var name in _userFiles)
            {
                if (name.IsSelected == false)
                    continue;

                var fileName = name.Id;

                var findFormat = new FindFileFormat();

                var type = findFormat.FindFormat(name.Name);

                var memoryStream = new MemoryStream();

                var downloadFile = new DownloadAzureFile();

                stream = downloadFile.DownloadFileAsync(fileName, _userId).Result;

                await stream.CopyToAsync(memoryStream);

                await stream.FlushAsync();

                switch (type)
                {
                    case ".txt":
                        Response.ContentType = "text/plain";
                        await HttpContext.Response.Body.WriteAsync(memoryStream.ToArray());

                        break;
                    case ".pdf":
                        Response.ContentType = "Application/pdf";
                        await Response.Body.WriteAsync(memoryStream.ToArray());

                        break;
                    case ".doc":
                        var docDocument = new WordDocument(memoryStream, FormatType.Docx);
                        var docRenderer = new DocIORenderer();

                        docRenderer.Settings.AutoTag = true;
                        docRenderer.Settings.PreserveFormFields = true;
                        docRenderer.Settings.ExportBookmarks = ExportBookmarkType.Headings;

                        var pdfDocDocument = docRenderer.ConvertToPDF(docDocument);
                        var docMemoryStream = new MemoryStream();

                        pdfDocDocument.Save(docMemoryStream);

                        Response.ContentType = "Application/pdf";

                        docMemoryStream.Position = 0;
                        await HttpContext.Response.Body.WriteAsync(docMemoryStream.ToArray());

                        break;
                    case ".png":
                        Response.ContentType = "image/png";
                        await HttpContext.Response.Body.WriteAsync(memoryStream.ToArray());

                        break;
                    case ".docx":
                        var docxDocument = new WordDocument(memoryStream, FormatType.Docx);
                        var docxRenderer = new DocIORenderer();

                        docxRenderer.Settings.AutoTag = true;
                        docxRenderer.Settings.PreserveFormFields = true;
                        docxRenderer.Settings.ExportBookmarks = ExportBookmarkType.Headings;

                        var pdfDocxDocument = docxRenderer.ConvertToPDF(docxDocument);
                        var docxMemoryStream = new MemoryStream();

                        pdfDocxDocument.Save(docxMemoryStream);

                        Response.ContentType = "Application/pdf";

                        docxMemoryStream.Position = 0;
                        await HttpContext.Response.Body.WriteAsync(docxMemoryStream.ToArray());

                        break;
                    case ".jpg":
                        Response.ContentType = "image/jpeg";
                        await HttpContext.Response.Body.WriteAsync(memoryStream.ToArray());

                        break;
                    case ".jpeg":
                        Response.ContentType = "image/jpeg";
                        await HttpContext.Response.Body.WriteAsync(memoryStream.ToArray());

                        break;
                    default:
                        await Response.WriteAsync("Sorry but we can't open this file :(");

                        break;
                }
                //

                break;
            }
        }

        [Authorize]
        public ActionResult Rename(string newName)
        {
            ViewData["Rename"] = newName;
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Needs refactoring
            if (_searchResult == null)
            {
                foreach (var name in _userFiles)
                {
                    if (name.IsSelected == true)
                    {
                        var toRename = _applicationDbContext.Files!.Find(name.Id);

                        if (toRename == null)
                            continue;

                        var fileFormat = new FindFileFormat();
                        var type = fileFormat.FindFormat(toRename.FilesId);

                        Files? modified = toRename;

                        newName = newName.Replace('.', '_');

                        modified.Name = newName + type;

                        _applicationDbContext.Files.Update(toRename).OriginalValues.SetValues(modified);

                        _userFiles.Find(file => file.Id == toRename.FilesId)!.Name = modified.Name;

                        _userFiles.Find(file => file.Id == toRename.FilesId)!.IsSelected = false;

                        _applicationDbContext.SaveChanges();
                    }
                }

                return View("AccessStorage", _userFiles);
            }
            else
            {
                foreach (var name in _searchResult)
                {
                    if (name.IsSelected == true)
                    {
                        var toRename = _applicationDbContext.Files!.Find(name.Id);

                        if (toRename == null)
                            continue;

                        var fileFormat = new FindFileFormat();
                        var type = fileFormat.FindFormat(toRename.FilesId);

                        Files? modified = toRename;

                        newName = newName.Replace('.', '_');

                        modified.Name = newName + type;

                        _applicationDbContext.Files.Update(toRename).OriginalValues.SetValues(modified);

                        _searchResult.Find(file => file.Id == toRename.FilesId)!.Name = modified.Name;

                        _searchResult.Find(file => file.Id == toRename.FilesId)!.IsSelected = false;

                        _applicationDbContext.SaveChanges();
                    }
                }

                return View("AccessStorage", _searchResult);
            }
            //
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
            var newName = GetUniqueName(receivedFile.MyFile!.FileName);

            var file = new Files(newName, receivedFile.MyFile.FileName);

            await uploadHandler.UploadFileAsync(receivedFile, file, Guid.NewGuid().ToString(), _applicationDbContext);

            return _tempFilesContainerLink + file.FilesId;
        }

        private async Task SinglePermFile(UploadInstance receivedFile, UploadPermFile uploadHandler)
        {
            var newName = GetUniqueName(receivedFile.MyFile!.FileName);

            var file = new Files(newName, receivedFile.MyFile.FileName, _userId!, false);

            await uploadHandler.UploadFileAsync(receivedFile, file, _userId!, _applicationDbContext);
        }

        private void FileClick(int position, List<UserFile> files)
        {
            files.ElementAt(position).IsSelected ^= true;
        }

        private void FilterFavourite()
        {
            _searchResult = _userFiles.Where(file => file.IsFavourite == true).ToList();
        }

        private string GetUniqueName(string oldName)
        {
            return Guid.NewGuid().ToString() + new FindFileFormat().FindFormat(oldName);
        }

        private enum SearchRedirect
        {
            ShowSearchResult,
            ShowUserFiles,
            UpdateUserFiles
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