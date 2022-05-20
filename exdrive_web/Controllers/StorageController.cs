using exdrive_web.Models;

using GroupDocs.Viewer;
using GroupDocs.Viewer.Options;

using JWTAuthentication.Authentication;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        private static readonly string _tmpFilesPath = "C:\\Users\\Public\\tmpfiles\\";
        private static readonly string _getLinkArchive = "C:\\Users\\Public\\getlink\\";
        private static readonly string _readerOutputPath = "C:\\Users\\Public\\reader\\Output";
        private static readonly string _readerInputPath = "C:\\Users\\Public\\reader\\Input";

        public StorageController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        [Authorize]
        public IActionResult AccessStorage()
        {
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
            _searchResult = _nameInstances.Where(x => x.IsFavourite == true).ToList();
            if (_searchResult.Count > 0)
                return View("AccessStorage", _searchResult);
            else
                return View("AccessStorage", _searchResult);
        }
        public IActionResult SingleFile()
        {
            return View(new UploadInstance());
        }
        [HttpPost]
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

            string newname = Guid.NewGuid().ToString() + ExFunctions.FindFormat(_file.MyFile.FileName);
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

            TempData["AlertMessage"] = "https://exdrivefiles.blob.core.windows.net/botfiles/" + files.FilesId;
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

            string newname = Guid.NewGuid().ToString() + ExFunctions.FindFormat(_file.MyFile.FileName);
            Files files = new(newname, _file.MyFile.FileName, _userId, false);

            try
            {
                await UploadPermAsync.UploadFileAsync(_file, files, _userId, _applicationDbContext);
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
                        await exdrive_web.Models.Trashcan.DeleteFile(name.Id, _userId);
                }
                return RedirectToAction("AccessStorage", "Storage");
            }

            // creating new list to preserve search results
            // function adds files that are not marked for deletion newsearch List
            int i = 0;
            List<NameInstance> newsearch = new List<NameInstance>();

            foreach (var name in _searchResult)
            {
                if (name.IsSelected == true)
                {
                    await exdrive_web.Models.Trashcan.DeleteFile(name.Id, _userId);
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


                TempData["LinkGet"] = "https://exdrivefiles.blob.core.windows.net/botfiles/" + file.FilesId;
                //return RedirectToAction("AccessStorage", "Storage", _nameInstances);
                return View("Trashcan", _nameInstances);
            }
        }

        [HttpPost]
        public IActionResult ReadFile()
        {
            Stream stream;
            FileStreamResult? fsResult = null;
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int i = -1;
            foreach (var name in _nameInstances)
            {
                i++;
                if (name.IsSelected == false)
                    continue;

                stream = DownloadAzureFile.DownloadFile(_nameInstances.ElementAt(i).Id, _userId);

                string outputDirectory = Path.Combine(_readerOutputPath, _userId);
                string inputDirectory = Path.Combine(_readerInputPath, _userId);

                System.IO.Directory.CreateDirectory(outputDirectory);
                System.IO.Directory.CreateDirectory(inputDirectory);

                string outputFilePath = Path.Combine(outputDirectory, "output " + name.Id);
                using (var filestream = System.IO.File.Create(Path.Combine(inputDirectory, name.Id)))
                {
                    stream.CopyTo(filestream);
                }

                using (Viewer viewer = new(Path.Combine(inputDirectory, name.Id)))
                {
                    PdfViewOptions options = new PdfViewOptions(outputFilePath);
                    try
                    {
                        viewer.View(options);
                    }
                    catch (Exception)
                    {
                        return RedirectToAction("AccessStorage", "Storage");
                    }
                }

                var fileStream = new FileStream(outputFilePath, FileMode.Open, FileAccess.Read);

                MemoryStream file = new();
                fileStream.CopyTo(file);

                file.Position = 0;
                fsResult = new FileStreamResult(file, "application/pdf");
                fileStream.Dispose();

                System.IO.Directory.Delete(inputDirectory, true);
                System.IO.Directory.Delete(outputDirectory, true);

                break;
            }
            return fsResult;
        }

        [HttpPost, DisableRequestSizeLimit]
        public ActionResult UploadTempFileBot()
        {
            if (Request.ContentLength == 0 || Request.ContentLength == null)
                return BadRequest("File for upload is empty");

            Microsoft.Extensions.Primitives.StringValues vs;
            Request.Headers.TryGetValue("file-name", out vs);
            string name = vs.First();

            Files file = new(Guid.NewGuid().ToString() + ExFunctions.FindFormat(name),
                name, "*", true);

            Stream stream = Request.Body;

            try
            {
                UploadTempBotAsync.UploadFileAsync(stream, (long)Request.ContentLength, file).Wait();
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
            response.WriteAsync("https://exdrivefiles.blob.core.windows.net/botfiles/" + file.FilesId);

            return Ok(response);
        }
    }
}