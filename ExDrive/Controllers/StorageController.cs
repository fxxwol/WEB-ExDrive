﻿using System.IO.Compression;
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
using Microsoft.EntityFrameworkCore;

namespace ExDrive.Controllers
{
    public class StorageController : Controller
    {
        private static List<NameInstance> _nameInstances = new();
        private static List<NameInstance>? _searchResult = new();

        private IServiceScopeFactory _serviceScopeFactory;

        private static ApplicationDbContext _applicationDbContext = new();

        private static string? _userId;
        private static bool _isDeleted = false;
        private static bool _isFavourite = false;

        private readonly string _tmpFilesPath = "C:\\Users\\Public\\tmpfiles\\";
        private readonly string _getLinkArchive = "C:\\Users\\Public\\getlink\\";
        private readonly string _tempFilesContainerLink = "https://exdrivefile.blob.core.windows.net/botfiles/";
        private readonly string _trashcanContainerName = "trashcan";

        public StorageController(ApplicationDbContext applicationDbContext, IServiceScopeFactory serviceScopeFactory)
        {
            _applicationDbContext = applicationDbContext;
            _serviceScopeFactory = serviceScopeFactory;
        }

        [Authorize]
        public IActionResult AccessStorage()
        {
            _isFavourite = false;
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _nameInstances = new List<NameInstance>(UserFilesDB.GetUserFilesDB(_userId));
            _searchResult = null;

            return View(_nameInstances);
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
                    return View("AccessStorage", _nameInstances);
                }

                _isDeleted = false;
                return RedirectToAction("AccessStorage", "Storage");
            }

            if (_isFavourite == true)
            {
                _searchResult = _nameInstances.Where(x => x.Name.Contains(searchString) && x.IsFavourite == true).ToList();
                if (_searchResult.Count > 0)
                {
                    return View("AccessStorage", _searchResult);
                }
                if (_isDeleted == false)
                {
                    return View("AccessStorage", _searchResult);
                }
            }

            // checking if user files' names contain search request
            _searchResult = _nameInstances.Where(x => x.Name.Contains(searchString)).ToList();
            if (_searchResult.Count > 0)
            {
                return View("AccessStorage", _searchResult);
            }

            // if file wasn't deleted, returning old view
            if (_isDeleted == false)
            {
                return View("AccessStorage", _nameInstances);
            }

            // if file is deleted, generating new List
            _isDeleted = false;
            return RedirectToAction("AccessStorage", "Storage");
        }

        [Authorize]
        public IActionResult FilterFav()
        {
            _isFavourite = true;
            if (_searchResult != null)
            {
                return RedirectToAction("AccessStorage", _nameInstances);
            }
            _searchResult = _nameInstances.Where(x => x.IsFavourite == true).ToList();
            return View("AccessStorage", _searchResult);
        }
        public IActionResult SingleFile()
        {
            return View(new UploadInstance());
        }
        public ActionResult Favourite()
        {
            foreach (var name in _nameInstances)
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

            try
            {
                await UploadTempAsync.UploadFileAsync(_file, file, _applicationDbContext);
            }
            catch (Exception)
            {
                TempData["AlertMessage"] = "Failed to upload: file may contain viruses";
                return RedirectToAction("Index", "Home");
            }

            TempData["AlertMessage"] = _tempFilesContainerLink + file.FilesId;
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
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

            try
            {
                await UploadPermAsync.UploadFileAsync(_file, file, _userId, _applicationDbContext);
            }
            catch (Exception)
            {

            }

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
                    _nameInstances.ElementAt(position).IsSelected ^= true;
                    return View("AccessStorage", _nameInstances);
                }

                _searchResult.ElementAt(position).IsSelected ^= true;
                return View("AccessStorage", _searchResult);
            }
            catch (Exception)
            {
                if (_searchResult == null)
                {
                    return View("AccessStorage", _nameInstances);
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
                foreach (var name in _nameInstances)
                {
                    if (name.IsSelected == true)
                    {
                        var trashcan = new Trashcan(_trashcanContainerName);
                        await trashcan.DeleteFile(name.Id, _userId, _applicationDbContext);
                    }
                }
                return RedirectToAction("AccessStorage", "Storage");
            }

            // creating new list to preserve search results
            // function adds files that are not marked for deletion newsearch List
            int i = 0;
            List<NameInstance> newSearch = new();

            foreach (var name in _searchResult)
            {
                if (name.IsSelected == true)
                {
                    var trashcan = new Trashcan(_trashcanContainerName);
                    await trashcan.DeleteFile(name.Id, _userId, _applicationDbContext);
                }
                else
                    newSearch.Add(_searchResult.ElementAt(i));

                i++;
            }

            _searchResult = newSearch;
            return View("AccessStorage", _searchResult);
        }
        public async Task<ActionResult> DownloadFiles()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var zipName = $"archive-{DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss")}.zip";

            System.IO.Directory.CreateDirectory(Path.Combine(_tmpFilesPath, _userId));

            if (_searchResult == null)
            {
                int i = -1;
                foreach (var name in _nameInstances)
                {
                    i++;

                    if (name.IsSelected == false)
                        continue;

                    using (var fileStream = new FileStream(Path.Combine(_tmpFilesPath + _userId,
                                                                        _nameInstances.ElementAt(i).Name),
                                                                        FileMode.Create, FileAccess.Write))
                    {
                        var downloadFile = new DownloadAzureFile();
                        
                        var memoryStream = await downloadFile.DownloadFile(_nameInstances.ElementAt(i).Id, _userId);

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

                    using (var fileStream = new FileStream(Path.Combine(_tmpFilesPath + _userId,
                                                                        _searchResult.ElementAt(i).Name),
                                                                        FileMode.Create, FileAccess.Write))
                    {
                        var downloadFile = new DownloadAzureFile();

                        var memoryStream = await downloadFile.DownloadFile(_searchResult.ElementAt(i).Id, _userId);

                        memoryStream.Position = 0;

                        await memoryStream.CopyToAsync(fileStream);

                        await memoryStream.FlushAsync();
                    }
                }
            }

            var files = Directory.GetFiles(Path.Combine(_tmpFilesPath, _userId)).ToList();

            if (files.Count < 1 && _searchResult == null)
            {
                return View("AccessStorage", _nameInstances);
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

        public async Task<string?> GetLink()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var zipName = $"archive-{DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss")}.zip";

            Directory.CreateDirectory(Path.Combine(_getLinkArchive, _userId));

            if (_searchResult == null)
            {
                int i = -1;
                foreach (var name in _nameInstances)
                {
                    i++;

                    if (name.IsSelected == false)
                        continue;

                    using (var fileStream = new FileStream(Path.Combine(_getLinkArchive + _userId,
                                                                        _nameInstances.ElementAt(i).Name),
                                                                        FileMode.Create, FileAccess.Write))
                    {
                        var downloadFile = new DownloadAzureFile();

                        var memoryStream = await downloadFile.DownloadFile(_nameInstances.ElementAt(i).Id, _userId);

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

                    using (var fileStream = new FileStream(Path.Combine(_getLinkArchive + _userId,
                                                                        _searchResult.ElementAt(i).Name),
                                                                        FileMode.Create, FileAccess.Write))
                    {
                        var downloadFile = new DownloadAzureFile();

                        var memoryStream = await downloadFile.DownloadFile(_searchResult.ElementAt(i).Id, _userId);

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

                await UploadTempAsync.UploadFileAsync(memoryStream, file, _applicationDbContext);

                Directory.Delete(Path.Combine(_getLinkArchive, _userId), true);

                return _tempFilesContainerLink + file.FilesId;
            }
        }


        [HttpPost]
        public async void ReadFile()
        {
            Stream stream;
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            foreach (var name in _nameInstances)
            {
                if (name.IsSelected == false)
                    continue;

                var fileName = name.Id;

                var findFormat = new FindFileFormat();

                var type = findFormat.FindFormat(name.Name);

                var memoryStream = new MemoryStream();

                var downloadFile = new DownloadAzureFile();

                stream = downloadFile.DownloadFile(fileName, _userId).Result;

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
        public ActionResult Rename(string newName)
        {
            ViewData["Rename"] = newName;
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (_searchResult == null)
            {
                foreach (var name in _nameInstances)
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

                        _nameInstances.Find(file => file.Id == toRename.FilesId)!.Name = modified.Name;

                        _applicationDbContext.SaveChanges();
                    }
                }

                return View("AccessStorage", _nameInstances);
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
                await UploadTempBotAsync.UploadFileAsync(stream, (long)Request.ContentLength, file, _applicationDbContext);
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
    }
}