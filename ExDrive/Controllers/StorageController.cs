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
        public IActionResult AccessStorage()
        {
            _isFavourite = false;
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var getUserFiles = new UserFilesDB();
            _userFiles = new List<UserFile>(getUserFiles.GetUserFilesDB(_userId));

            _searchResult = null;

            return View(_userFiles);
        }
        public IActionResult Trashcan()
        {
            return View();
        }

        [Authorize]
        public IActionResult Search(string searchString)
        {
            ViewData["GetFiles"] = searchString;

            if (searchString == null && _searchResult != null)
            {
                return View("AccessStorage", _searchResult);
            }

            if (searchString == null)
            {
                _searchResult = null;

                if (_isDeleted == false)
                {
                    return View("AccessStorage", _userFiles);
                }

                _isDeleted = false;
                return RedirectToAction("AccessStorage", "Storage");
            }

            if (_isFavourite == true)
            {
                _searchResult = _userFiles.Where(file => file.Name.Contains(searchString) && file.IsFavourite == true).ToList();
                if (_searchResult.Count > 0)
                {
                    return View("AccessStorage", _searchResult);
                }
                if (_isDeleted == false)
                {
                    return View("AccessStorage", _searchResult);
                }
            }

            _searchResult = _userFiles.Where(file => file.Name.Contains(searchString)).ToList();
            if (_searchResult.Count > 0)
            {
                return View("AccessStorage", _searchResult);
            }

            if (_isDeleted == false)
            {
                return View("AccessStorage", _userFiles);
            }

            _isDeleted = false;
            return RedirectToAction("AccessStorage", "Storage");
        }

        [Authorize]
        public IActionResult FilterFav()
        {
            _isFavourite = true;

            if (_searchResult != null)
            {
                return RedirectToAction("AccessStorage", _userFiles);
            }

            _searchResult = _userFiles.Where(file => file.IsFavourite == true).ToList();

            return View("AccessStorage", _searchResult);
        }
        public IActionResult SingleFile()
        {
            return View(new UploadInstance());
        }

        [Authorize]
        public ActionResult Favourite()
        {
            foreach (var name in _userFiles)
            {
                if (name.IsSelected == true)
                {
                    Files? favourite = _applicationDbContext.Files!.Find(name.Id);
                    if (favourite != null)
                    {
                        Files? modified = favourite;
                        modified.Favourite ^= true;
                        _applicationDbContext.Files.Update(favourite).OriginalValues.SetValues(modified);
                    }
                    _applicationDbContext.SaveChanges();

                }
            }
            return RedirectToAction("AccessStorage", "Storage");
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = 629145600)]
        public async Task<IActionResult> SingleTempFile(UploadInstance _file)
        {
            if (_file.MyFile == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var findFormat = new FindFileFormat();

            var newName = Guid.NewGuid().ToString() + findFormat.FindFormat(_file.MyFile.FileName);

            var file = new Files(newName, _file.MyFile.FileName, "*", true);

            var uploadTempAsync = new UploadTempFile();

            try
            {
                await uploadTempAsync.UploadFileAsync(_file, file, Guid.NewGuid().ToString(), _applicationDbContext);
            }
            catch (Exception)
            {
                TempData["AlertMessage"] = "Failed to upload: file may contain viruses";

                await uploadTempAsync.DisposeAsync();

                return RedirectToAction("Index", "Home");
            }
            finally
            {
                await uploadTempAsync.DisposeAsync();
            }

            TempData["AlertMessage"] = _tempFilesContainerLink + file.FilesId;
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = 629145600)]
        public async Task<IActionResult> SinglePermFile(UploadInstance _file)
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (_userId == null || _file.MyFile == null)
                return RedirectToAction("AccessStorage", "Storage");

            var findFormat = new FindFileFormat();

            var newName = Guid.NewGuid().ToString() + findFormat.FindFormat(_file.MyFile.FileName);

            var file = new Files(newName, _file.MyFile.FileName, _userId, false);

            var uploadPermFile = new UploadPermFile();

            await uploadPermFile.UploadFileAsync(_file, file, _userId, _applicationDbContext);

            await uploadPermFile.DisposeAsync();

            return RedirectToAction("AccessStorage", "Storage");
        }
        public ActionResult FileClick(string afile)
        {
            try
            {
                var position = Int32.Parse(afile);

                // if there was no search, file from main List is selected
                // (user is not in search mode)
                if (_searchResult == null)
                {
                    _userFiles.ElementAt(position).IsSelected ^= true;
                    return View("AccessStorage", _userFiles);
                }

                _searchResult.ElementAt(position).IsSelected ^= true;
                return View("AccessStorage", _searchResult);
            }
            catch (Exception)
            {
                if (_searchResult == null)
                {
                    return View("AccessStorage", _userFiles);
                }

                return View("AccessStorage", _searchResult);
            }
        }

        public async Task<ActionResult> Delete()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _isDeleted = true;

            // if there was no search, files from main List are deleted
            // (user is not in search mode)
            if (_searchResult == null)
            {
                foreach (var name in _userFiles)
                {
                    if (name.IsSelected == true)
                    {
                        var trashcan = new Trashcan(_trashcanContainerName);
                        await trashcan.DeleteFileAsync(name.Id, _userId, _applicationDbContext);
                    }
                }
                return RedirectToAction("AccessStorage", "Storage");
            }

            // creating new list to preserve search results
            // function adds files that are not marked for deletion newsearch List
            int i = 0;
            List<UserFile> newSearch = new();

            foreach (var name in _searchResult)
            {
                if (name.IsSelected == true)
                {
                    var trashcan = new Trashcan(_trashcanContainerName);
                    await trashcan.DeleteFileAsync(name.Id, _userId, _applicationDbContext);
                }
                else
                    newSearch.Add(_searchResult.ElementAt(i));

                i++;
            }

            _searchResult = newSearch;
            return View("AccessStorage", _searchResult);
        }

        [Authorize]
        public async Task<ActionResult> DownloadFiles()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var zipName = $"archive-{DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss")}.zip";

            Directory.CreateDirectory(Path.Combine(_tmpFilesPath, _userId));

            if (_searchResult == null)
            {
                int i = -1;
                foreach (var name in _userFiles)
                {
                    i++;

                    if (name.IsSelected == false)
                        continue;

                    var downloadedFiles = Directory.GetFiles(Path.Combine(_tmpFilesPath, _userId)).ToList();

                    var fileName = String.Empty;

                    var currentName = Path.Combine(_tmpFilesPath + _userId, _userFiles.ElementAt(i).Name);

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

                    using (var fileStream = new FileStream(Path.Combine(_tmpFilesPath + _userId,
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

                    var downloadedFiles = Directory.GetFiles(Path.Combine(_tmpFilesPath, _userId)).ToList();

                    var fileName = String.Empty;

                    if (downloadedFiles.Contains(Path.Combine(_tmpFilesPath + _userId, _searchResult.ElementAt(i).Name)))
                    {
                        var findFormat = new FindFileFormat();

                        fileName = _searchResult.ElementAt(i).NoFormat + $"({i})" +
                            findFormat.FindFormat(_searchResult.ElementAt(i).Name);
                    }
                    else
                    {
                        fileName = _searchResult.ElementAt(i).Name;
                    }

                    using (var fileStream = new FileStream(Path.Combine(_tmpFilesPath + _userId,
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

            var files = Directory.GetFiles(Path.Combine(_tmpFilesPath, _userId)).ToList();

            if (files.Count < 1 && _searchResult == null)
            {
                return View("AccessStorage", _userFiles);
            }

            if (files.Count < 1)
            {
                return View("AccessStorage", _searchResult);
            }

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
        }

        [Authorize]
        public async Task<string?> GetLink()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var zipName = $"archive-{DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss")}.zip";

            Directory.CreateDirectory(Path.Combine(_getLinkArchive, _userId));

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

            var files = Directory.GetFiles(Path.Combine(_getLinkArchive, _userId)).ToList();

            if (files.Count < 1)
            {
                return null;
            }

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
        }

        [HttpPost]
        [Authorize]
        public async void ReadFile()
        {
            Stream stream;
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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

                break;
            }
        }

        [Authorize]
        public ActionResult Rename(string newName)
        {
            ViewData["Rename"] = newName;
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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

                        _applicationDbContext.SaveChanges();
                    }
                }

                return View("AccessStorage", _searchResult);
            }
        }

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
        public StorageController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        private static List<UserFile> _userFiles = new();
        private static List<UserFile>? _searchResult = new();

        private static ApplicationDbContext _applicationDbContext = new();

        private static string? _userId;
        private static bool _isDeleted = false;
        private static bool _isFavourite = false;

        private static readonly string _tmpFilesPath = "C:\\Users\\Public\\tmpfiles\\";
        private static readonly string _getLinkArchive = "C:\\Users\\Public\\getlink\\";
        private static readonly string _tempFilesContainerLink = "https://exdrivefile.blob.core.windows.net/botfiles/";
        private static readonly string _trashcanContainerName = "trashcan";
    }
}