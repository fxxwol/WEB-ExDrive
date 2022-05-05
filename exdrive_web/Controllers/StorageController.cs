using exdrive_web.Models;
using JWTAuthentication.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            var file = _file.MyFile;
            if (file != null)
            {
                var dir = _webHostEnvironment.ContentRootPath;
                string filename = file.FileName;

                string format = "";
                for (int i = filename.LastIndexOf('.'); i < filename.Length; i++)
                    format += file.FileName.ElementAt(i);

                string newname = Guid.NewGuid().ToString() + format;
                using (var fileStream = new FileStream(Path.Combine(dir, newname), FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(fileStream);
                }

                MemoryStream ms = new MemoryStream();
                var filems = file.OpenReadStream();
                filems.CopyToAsync(ms).Wait();

                Files files = new Files();
                files.Name = filename;
                files.FilesId = newname;
                files.IsTemporary = true;
                files.HasAccess = "*";


                string downloadLink = "https://exdrivefiles.blob.core.windows.net/botfiles/" + files.FilesId;
                await UploadTempAsync.UploadFileAsync(file, dir, files, ms);
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
                var file = _file.MyFile;
                var dir = _webHostEnvironment.ContentRootPath;
                string filename = file.FileName;

                string format = "";
                for (int i = filename.LastIndexOf('.'); i < filename.Length; i++)
                    format += file.FileName.ElementAt(i);

                string newname = Guid.NewGuid().ToString() + format;
                using (var fileStream = new FileStream(Path.Combine(dir, newname), FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(fileStream);
                }

                MemoryStream ms = new MemoryStream();
                var filems = file.OpenReadStream();
                filems.CopyToAsync(ms).Wait();

                Files files = new Files();
                files.Name = filename;
                files.FilesId = newname;
                files.IsTemporary = false;
                files.HasAccess = _userId;

                await UploadPermAsync.UploadFileAsync(file, dir, files, ms, _userId);
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

        public async Task<ActionResult> Delete(string file)
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
    }
}
