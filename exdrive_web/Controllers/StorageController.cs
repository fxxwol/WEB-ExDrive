using exdrive_web.Models;
using JWTAuthentication.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security.Claims;

namespace exdrive_web.Controllers
{

    public class StorageController : Controller
    {
        private IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _db;
        private string? _userId;
        private static List<NameInstance> _nameInstances = new List<NameInstance>();
        private static List<NameInstance> _searchResult = new List<NameInstance>();
        private static bool _isDeleted = false;
        public StorageController(IWebHostEnvironment environment, ApplicationDbContext db)
        {
            _webHostEnvironment = environment;
            _db = db;
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
                if (_isDeleted)
                {
                    _isDeleted = false;
                    return RedirectToAction("AccessStorage", "Storage");
                }
                else
                    return View("AccessStorage", _nameInstances);

            }

            _searchResult = _nameInstances.Where(x => x.NoFormat.Equals(searchString)).ToList();
            if (_searchResult.Count > 0)
                return View("AccessStorage", _searchResult);
            else
            {
                _searchResult = _nameInstances.Where(x => x.Name.Equals(searchString)).ToList();
                if (_searchResult.Count > 0)
                    return View("AccessStorage", _searchResult);
                else
                {
                    if (_isDeleted)
                    {
                        _isDeleted = false;
                        return RedirectToAction("AccessStorage", "Storage");
                    }
                    else
                        return View("AccessStorage", _nameInstances);
                }
            }
        }
        public IActionResult SingleFile()
        {
            return View(new UploadInstance());
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = 629145600)]
        public async Task<IActionResult> SingleFile(UploadInstance _file)
        {
            if (_file.MyFile != null)
            {
                string format = "";
                for (int i = _file.MyFile.FileName.LastIndexOf('.'); i < _file.MyFile.FileName.Length; i++)
                    format += _file.MyFile.FileName.ElementAt(i);

                string newname = Guid.NewGuid().ToString() + format;
                Files files = new Files(newname, _file.MyFile.FileName, "*", true);

                string downloadLink = "https://exdrivefiles.blob.core.windows.net/botfiles/" + files.FilesId;
                await UploadTempAsync.UploadFileAsync(_file, files);
                TempData["AlertMessage"] = downloadLink;
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = 629145600)]
        public async Task<IActionResult> SinglePermFile(UploadInstance _file)
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (_file.MyFile != null && !string.IsNullOrEmpty(_userId))
            {
                string format = "";
                for (int i = _file.MyFile.FileName.LastIndexOf('.'); i < _file.MyFile.FileName.Length; i++)
                    format += _file.MyFile.FileName.ElementAt(i);

                string newname = Guid.NewGuid().ToString() + format;
                Files files = new Files(newname, _file.MyFile.FileName, _userId, false);

                await UploadPermAsync.UploadFileAsync(_file, files, _userId);
            }
            return RedirectToAction("AccessStorage", "Storage");
        }
        public ActionResult FileClick(string afile)
        {
            int position = Int32.Parse(afile);
            _isDeleted = true;

            if (_searchResult == null)
            {
                _nameInstances.ElementAt(position).IsSelected ^= true;
                return View("AccessStorage", _nameInstances);
            }
            else
            {
                _searchResult.ElementAt(position).IsSelected ^= true;
                return View("AccessStorage", _searchResult);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Delete()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (_searchResult == null)
            {
                foreach (var name in _nameInstances)
                {
                    if (name.IsSelected == true)
                        await exdrive_web.Models.Trashcan.DeleteFile(name.Id, _userId);
                }
                return RedirectToAction("AccessStorage", "Storage");
            }
            else
            {
                int i = 0;
                List<NameInstance> newsearch = new List<NameInstance>();
                foreach (var name in _searchResult)
                {
                    if (name.IsSelected == true)
                    {
                        await exdrive_web.Models.Trashcan.DeleteFile(name.Id, _userId);
                    }
                    else
                    {
                        newsearch.Add(_searchResult.ElementAt(i));
                    }
                    i++;
                }
                _searchResult = newsearch;
                return View("AccessStorage", _searchResult);
            }
        }

        public ActionResult DownloadFiles()
        {

            var zipName = $"archive-{DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss")}.zip";
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            System.IO.Directory.CreateDirectory(_userId);
            int i = 0;
            foreach (var name in _nameInstances)
            {
                if (name.IsSelected == true)
                    using (var fileStream = new FileStream(Path.Combine(_webHostEnvironment.ContentRootPath + _userId, _nameInstances.ElementAt(i).Name), FileMode.Create, FileAccess.Write))
                        DownloadAzureFile.DownloadFile(_nameInstances.ElementAt(i).Id, _userId).CopyTo(fileStream);
                i++;
            }

            var files = Directory.GetFiles(Path.Combine(_webHostEnvironment.ContentRootPath, _userId)).ToList();
            if (files.Count < 1)
                return View("AccessStorage", _nameInstances);

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    files.ForEach(file =>
                    {
                        archive.CreateEntryFromFile(file, file.Replace(Path.Combine(_webHostEnvironment.ContentRootPath + _userId) + "\\", string.Empty));
                    });
                }

                System.IO.Directory.Delete(Path.Combine(_webHostEnvironment.ContentRootPath, _userId), true);
                return File(memoryStream.ToArray(), "application/zip", zipName);
            }
        }
        public IActionResult ReadFiles()
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            List<string> readtext = new List<string>();
            System.IO.Directory.CreateDirectory(_userId);
            Stream stream;
            string? line;

            int i = 0;
            foreach (var name in _nameInstances)
            {
                if (name.IsSelected == true) // && name.Name.EndsWith("txt")
                {
                    stream = DownloadAzureFile.DownloadFile(_nameInstances.ElementAt(i).Id, _userId);
                    TextReader tr = new StreamReader(stream);

                    do
                    {
                        line = tr.ReadLine();
                        if (line != null)
                            readtext.Add(line);
                    } while (line != null);
                    
                    tr.Dispose();
                    break;
                }
                i++;
            }

            return View("ReadTXT", readtext);
        }
    }
}

