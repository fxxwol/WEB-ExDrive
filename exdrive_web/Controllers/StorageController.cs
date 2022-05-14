﻿using exdrive_web.Models;
using GroupDocs.Viewer;
using GroupDocs.Viewer.Interfaces;
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
        private string? _userId;
        private static List<NameInstance> _nameInstances = new List<NameInstance>();
        private static List<NameInstance>? _searchResult = new List<NameInstance>();
        private static bool _isDeleted = false;
        public StorageController(IWebHostEnvironment environment, ApplicationDbContext db)
        {

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

            // checking a file's name without format first
            _searchResult = _nameInstances.Where(x => x.NoFormat.Equals(searchString)).ToList();
            if (_searchResult.Count > 0)
                return View("AccessStorage", _searchResult);

            // checking a file's name with format
            _searchResult = _nameInstances.Where(x => x.Name.Equals(searchString)).ToList();
            if (_searchResult.Count > 0)
                return View("AccessStorage", _searchResult);

            // if file wasn't deleted, returning old view
            if (_isDeleted == false)
                return View("AccessStorage", _nameInstances);

            // if file is deleted, generating new List
            _isDeleted = false;
            return RedirectToAction("AccessStorage", "Storage");
        }
        public IActionResult SingleFile()
        {
            return View(new UploadInstance());
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
                await UploadTempAsync.UploadFileAsync(_file, files);
            }
            catch(Exception)
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
                await UploadPermAsync.UploadFileAsync(_file, files, _userId);
            }
            catch (Exception)
            { }

            return RedirectToAction("AccessStorage", "Storage");
        }
        public ActionResult FileClick(string afile)
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

        [HttpPost]
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

            System.IO.Directory.CreateDirectory(Path.Combine("C:\\Users\\Public\\tmpfiles\\", _userId));
            int i = -1;
            foreach (var name in _nameInstances)
            {
                i++;

                if (name.IsSelected == false)
                    continue;

                using (var fileStream = new FileStream(Path.Combine("C:\\Users\\Public\\tmpfiles\\" + _userId, _nameInstances.ElementAt(i).Name), FileMode.Create, FileAccess.Write))
                    DownloadAzureFile.DownloadFile(_nameInstances.ElementAt(i).Id, _userId).CopyTo(fileStream);
            }

            var files = Directory.GetFiles(Path.Combine("C:\\Users\\Public\\tmpfiles\\", _userId)).ToList();
            if (files.Count < 1)
                return View("AccessStorage", _nameInstances);

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    files.ForEach(file =>
                    {
                        archive.CreateEntryFromFile(file, file.Replace(Path.Combine("C:\\Users\\Public\\tmpfiles\\" + _userId) + "\\", string.Empty));
                    });
                }

                System.IO.Directory.Delete(Path.Combine("C:\\Users\\Public\\tmpfiles\\", _userId), true);

                return File(memoryStream.ToArray(), "application/zip", zipName);
            }
        }

        [HttpPost]
        public IActionResult ReadFile()
        {
            // Loading_a_License_from_a_Stream_Object();
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
                string outputDirectory = Path.Combine("C:\\Users\\Public\\reader\\Output", _userId);
                string inputDirectory = Path.Combine("C:\\Users\\Public\\reader\\Input", _userId);

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
                    viewer.View(options);
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

