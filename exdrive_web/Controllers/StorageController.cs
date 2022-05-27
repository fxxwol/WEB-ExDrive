using exdrive_web.Helpers;
using exdrive_web.Models;

using GroupDocs.Viewer;
using GroupDocs.Viewer.Options;

using JWTAuthentication.Authentication;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;

using System.IO.Compression;
using System.Security.Claims;

namespace exdrive_web.Controllers
{
    public class StorageController : Controller
    {
        private static List<NameInstance> _nameInstances = new List<NameInstance>();
        private static List<NameInstance>? _searchResult = new List<NameInstance>();

        private static ApplicationDbContext _applicationDbContext;

        private string? _userId;
        private static bool _isDeleted = false;
        private static bool _isFavourite = false;

        private readonly string _tmpFilesPath = "C:\\Users\\Public\\tmpfiles\\";
        private readonly string _getLinkArchive = "C:\\Users\\Public\\getlink\\";
        private readonly string _readerOutputPath = "C:\\Users\\Public\\reader\\Output";
        private readonly string _readerInputPath = "C:\\Users\\Public\\reader\\Input";
        private readonly string _tempFilesContainerLink = "https://exdrivefiles.blob.core.windows.net/botfiles/";

        public StorageController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
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

            if (searchString == null)
            {
                _searchResult = null;

                if (_isDeleted == false)
                    return View("AccessStorage", _nameInstances);

                _isDeleted = false;
                return RedirectToAction("AccessStorage", "Storage");
            }

            if (_isFavourite == true)
            {
                _searchResult = _nameInstances.Where(x => x.Name.Contains(searchString) && x.IsFavourite == true).ToList();
                if (_searchResult.Count > 0)
                    return View("AccessStorage", _searchResult);
                if (_isDeleted == false)
                    return View("AccessStorage", _searchResult);
            }
            
            // checking if user files' names contain search request
            _searchResult = _nameInstances.Where(x => x.Name.Contains(searchString)).ToList();
            if (_searchResult.Count > 0)
                return View("AccessStorage", _searchResult);

 
            // if file wasn't deleted, returning old view
            if (_isDeleted == false)
                return View("AccessStorage", _nameInstances);

            // if file is deleted, generating new List
            _isDeleted = false;
            return RedirectToAction("AccessStorage", "Storage");
        }

        [Authorize]
        public IActionResult FilterFav()
        {
            _isFavourite = true;
            if (_searchResult != null)
                return RedirectToAction("AccessStorage", _nameInstances);
            _searchResult = _nameInstances.Where(x => x.IsFavourite == true).ToList();
            return View("AccessStorage", _searchResult);
        }
        public IActionResult SingleFile()
        {
            return View(new UploadInstance());
        }
        public ActionResult Favourite()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            foreach (var name in _nameInstances)
            {
                if (name.IsSelected == true)
                {
                    Files? favourite = _applicationDbContext.Files.Find(name.Id);
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
                return RedirectToAction("Index", "Home");

            string newname = Guid.NewGuid().ToString() + FindFileFormat.FindFormat(_file.MyFile.FileName);
            Files files = new(newname, _file.MyFile.FileName, "*", true);

            try
            {
                await UploadTempAsync.UploadFileAsync(_file, files, _applicationDbContext);
            }
            catch (Exception)
            {
                TempData["AlertMessage"] = "Failed to upload: file may contain viruses";
                return RedirectToAction("Index", "Home");
            }

            TempData["AlertMessage"] = _tempFilesContainerLink + files.FilesId;
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = 629145600)]
        public async Task<IActionResult> SinglePermFile(UploadInstance file)
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (_userId == null || file.MyFile == null)
                return RedirectToAction("AccessStorage", "Storage");

            string newname = Guid.NewGuid().ToString() + FindFileFormat.FindFormat(file.MyFile.FileName);
            Files files = new(newname, file.MyFile.FileName, _userId, false);

            try
            {
                await UploadPermAsync.UploadFileAsync(file, files, _userId, _applicationDbContext);
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
                int position = Int32.Parse(afile);

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
                    return View("AccessStorage", _nameInstances);

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
                        await exdrive_web.Models.Trashcan.DeleteFile(name.Id, _userId, _applicationDbContext);
                }
                return RedirectToAction("AccessStorage", "Storage");
            }

            // creating new list to preserve search results
            // function adds files that are not marked for deletion newsearch List
            int i = 0;
            List<NameInstance> newsearch = new();

            foreach (var name in _searchResult)
            {
                if (name.IsSelected == true)
                {
                    await exdrive_web.Models.Trashcan.DeleteFile(name.Id, _userId, _applicationDbContext);
                }
                else
                    newsearch.Add(_searchResult.ElementAt(i));

                i++;
            }

            _searchResult = newsearch;
            return View("AccessStorage", _searchResult);
        }
        public ActionResult DownloadFiles()
        {

            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var zipName = $"archive-{DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss")}.zip";

            System.IO.Directory.CreateDirectory(Path.Combine(_tmpFilesPath, _userId));

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
                    DownloadAzureFile.DownloadFile(_nameInstances.ElementAt(i).Id, _userId).CopyTo(fileStream);
                }
            }

            var files = Directory.GetFiles(Path.Combine(_tmpFilesPath, _userId)).ToList();
            if (files.Count < 1)
                return View("AccessStorage", _nameInstances);

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

                System.IO.Directory.Delete(Path.Combine(_tmpFilesPath, _userId), true);

                return File(memoryStream.ToArray(), "application/zip", zipName);
            }
        }

        public async Task<IActionResult> GetLink()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var zipName = $"archive-{DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss")}.zip";

            System.IO.Directory.CreateDirectory(Path.Combine(_getLinkArchive, _userId));

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
                    DownloadAzureFile.DownloadFile(_nameInstances.ElementAt(i).Id, _userId).CopyTo(fileStream);
                }
            }

            var files = Directory.GetFiles(Path.Combine(_getLinkArchive, _userId)).ToList();
            if (files.Count < 1)
                return View("AccessStorage", _nameInstances);

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

                Files file = new(Guid.NewGuid().ToString() + ".zip", _userId + zipName, "*", true);

                await UploadTempAsync.UploadFileAsync(memoryStream, file, _applicationDbContext);

                System.IO.Directory.Delete(Path.Combine(_getLinkArchive, _userId), true);


                TempData["LinkGet"] = _tempFilesContainerLink + file.FilesId;
                
                return View("Trashcan", _nameInstances);
            }
        }

        
        [HttpPost]
        public void ReadFile()
        {
            Stream stream;
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
   
            foreach (var name in _nameInstances)
            {
                if (name.IsSelected == false)
                    continue;

                string fileName = name.Id;
                string type = "";

                MemoryStream ms = new();
                stream = DownloadAzureFile.DownloadFile(fileName, _userId);
                stream.CopyTo(ms);

                for (int i = name.Name.LastIndexOf('.') + 1; i < name.Name.Length; i++)
                {
                    type += name.Name.ElementAt(i);
                }
        
                switch (type)
                {
                    case "txt":
                        Response.ContentType = "text/plain";
                        HttpContext.Response.Body.Write(ms.ToArray());
                        break;
                    case "pdf":
                        Response.ContentType = "Application/pdf";
                        HttpContext.Response.Body.Write(ms.ToArray());
                        Response.CompleteAsync();
                        break;
                    case "doc":
                        var word = new WordDocument(ms, Syncfusion.DocIO.FormatType.Docx);
                        var docRenderer = new DocIORenderer();
                        docRenderer.Settings.AutoTag = true;
                        docRenderer.Settings.PreserveFormFields = true;
                        docRenderer.Settings.ExportBookmarks = ExportBookmarkType.Headings;
                        var pdfDocument = docRenderer.ConvertToPDF(word);
                        var memoryStream = new MemoryStream();
                        pdfDocument.Save(memoryStream);

                        memoryStream.Position = 0;
                        Response.ContentType = "Application/pdf";
                        HttpContext.Response.Body.Write(memoryStream.ToArray());
                        break;
                    case "png":
                        Response.ContentType = "image/png";
                        HttpContext.Response.Body.Write(ms.ToArray());
                        Response.CompleteAsync();
                        break;
                    case "docx": 
                        var wordDocument = new WordDocument(ms, Syncfusion.DocIO.FormatType.Docx);
                        var docIORenderer = new DocIORenderer();
                        docIORenderer.Settings.AutoTag = true;
                        docIORenderer.Settings.PreserveFormFields = true;
                        docIORenderer.Settings.ExportBookmarks = ExportBookmarkType.Headings;
                        var pdfDoc = docIORenderer.ConvertToPDF(wordDocument);
                        var memStream = new MemoryStream();
                        pdfDoc.Save(memStream);
                        memStream.Position = 0;
                        Response.ContentType = "Application/pdf";
                        HttpContext.Response.Body.Write(memStream.ToArray());
                        break;
                    case "jpg":
                        Response.ContentType = "image/jpeg";
                        HttpContext.Response.Body.Write(ms.ToArray());
                        Response.CompleteAsync();
                        break;
                    case "jpeg":
                        Response.ContentType = "image/jpeg";
                        HttpContext.Response.Body.Write(ms.ToArray());
                        Response.CompleteAsync();
                        break;
                    default:
                        Response.WriteAsync("Sorry but we can't open this file :(");
                        break;
                }
                break;
            }
        }
        public ActionResult Rename(string newName)
        {
            ViewData["Rename"] = newName;
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            foreach (var name in _nameInstances)
            {
                if (name.IsSelected == true)
                {
                    Files? toRename = _applicationDbContext.Files.Find(name.Id);
                    string type = "";
                    if (toRename != null)
                    {
                        for (int i = toRename.Name.LastIndexOf('.') + 1; i < toRename.Name.Length; i++)
                        {
                            type += toRename.Name.ElementAt(i);
                        }
                        Files? modified = toRename;
                        if (!newName.EndsWith(type))
                        {
                            modified.Name = newName + "." + type;
                            _applicationDbContext.Files.Update(toRename).OriginalValues.SetValues(modified);
                        }
                        else
                        {
                            modified.Name = newName;
                            _applicationDbContext.Files.Update(toRename).OriginalValues.SetValues(modified);
                        }
                    }
                    _applicationDbContext.SaveChanges();

                }
            }
            return RedirectToAction("AccessStorage", "Storage");
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

            Files file = new(Guid.NewGuid().ToString() + FindFileFormat.FindFormat(name),
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