using exdrive_web.Models;
using JWTAuthentication.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace exdrive_web.Controllers
{
    
    public class StorageController : Controller
    {
        private IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _db;
        public StorageController(IWebHostEnvironment environment, ApplicationDbContext db)
        {
            _webHostEnvironment = environment;
            _db = db;
        }
        [Authorize]
        public IActionResult AccessStorage()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AccessStorage(string filename)
        {
            ViewData["GetFiles"] = filename;
            var files = from x in _db.Files select x;

            if (!String.IsNullOrEmpty(filename))
            {
                files = files.Where(x => x.Name.Contains(filename));
            }
            return View(await files.AsNoTracking().ToListAsync());
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
                await UploadAsync.UploadFileAsync(file, dir, files, ms);
                TempData["AlertMessage"] = downloadLink;
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
