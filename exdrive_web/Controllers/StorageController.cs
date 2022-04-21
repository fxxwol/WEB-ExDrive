using exdrive_web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace exdrive_web.Controllers
{
    public class StorageController : Controller
    {
        private IWebHostEnvironment _webHostEnvironment;

        public StorageController(IWebHostEnvironment environment)
        {
            _webHostEnvironment = environment;
        }

        [Authorize]
        public IActionResult AccessStorage()
        {
            return View();
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = 629145600)]
        public async Task<IActionResult> SingleFile(IFormFile file)
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


            string storageLink = "https://exdrivefiles.blob.core.windows.net/botfiles/" + files.FilesId;
            await UploadAsync.UploadFileAsync(file, dir, files, ms);

            return RedirectToAction("Index", "Home");
        }
    }
}
