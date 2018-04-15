using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;
using System.IO;
using AspNetCoreFileProcessing.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Cors;

namespace AspNetCoreFileProcessing.Controllers
{
    [Route("api/[controller]")]
    public class FilesController : Controller
    {
        private const int _CONTENT_SIZE_IN_MB = 20;
        private static readonly byte[] _bytes;

        static FilesController()
        {
            _bytes = Helpers.GetRandomBytes(_CONTENT_SIZE_IN_MB);
        }

        [HttpGet]
        public IActionResult Ping()
        {
            return Json("OK");
        }

        [HttpPost("Upload")]
        public async Task<IActionResult> Upload()
        {
            var sw = Stopwatch.StartNew();

            var bufferSize = 4 * 1024;
            var totalBytes = await Helpers.ReadStream(Request.Body, bufferSize);

            sw.Stop();
            Helpers.PrintDuration("Upload", totalBytes, sw.Elapsed);

            return Ok();
        }

        [HttpPost("UploadMultipartUsingIFormFile")]
        public async Task<IActionResult> UploadMultipartUsingIFormFile(UploadMultipartModel model)
        {
            var sw = Stopwatch.StartNew();

            var bufferSize = 32 * 1024;
            var totalBytes = await Helpers.ReadStream(model.File.OpenReadStream(), bufferSize);

            sw.Stop();
            Helpers.PrintDuration($"UploadMultipartUsingIFormFile with Value={model.SomeValue}", totalBytes, sw.Elapsed);

            return Ok();
        }

        [HttpPost("UploadMultipartUsingReader")]
        public async Task<IActionResult> UploadMultipartUsingReader()
        {
            var sw = Stopwatch.StartNew();

            var boundary = GetBoundary(Request.ContentType);
            var reader = new MultipartReader(boundary, Request.Body, 80 * 1024);

            var valuesByKey = new Dictionary<string, string>();
            var totalBytes = 0;
            MultipartSection section;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var contentDispo = section.GetContentDispositionHeader();

                if (contentDispo.IsFileDisposition())
                {
                    var fileSection = section.AsFileSection();
                    var bufferSize = 32 * 1024;
                    byte[] buffer = new byte[bufferSize];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int read;
                        while ((read = fileSection.FileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                        }
                        var bytes = ms.ToArray();
                    }
                    totalBytes = await Helpers.ReadStream(fileSection.FileStream, bufferSize);
                }
                else if (contentDispo.IsFormDisposition())
                {
                    var formSection = section.AsFormDataSection();
                    var value = await formSection.GetValueAsync();
                    valuesByKey.Add(formSection.Name, value);
                }
            }

            sw.Stop();
            Helpers.PrintDuration($"UploadMultipartUsingReader with Value={valuesByKey["SomeValue"]}", totalBytes, sw.Elapsed);

            return Ok();
        }

        [HttpPost("UploadSql")]
        public async Task<IActionResult> UploadSql()
        {
            try
            {
                var boundary = GetBoundary(Request.ContentType);
                var reader = new MultipartReader(boundary, Request.Body, 80 * 1024);

                var valuesByKey = new Dictionary<string, string>();
                MultipartSection section;

                while ((section = await reader.ReadNextSectionAsync()) != null)
                {
                    var contentDispo = section.GetContentDispositionHeader();

                    if (contentDispo.IsFileDisposition())
                    {
                        var fileSection = section.AsFileSection();
                        byte[] buffer = new byte[16 * 1024];
                        using (MemoryStream ms = new MemoryStream())
                        {
                            int read;
                            while ((read = fileSection.FileStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                ms.Write(buffer, 0, read);
                            }
                            var bytes = ms.ToArray();
                        }
                        //SqlRepository repo = new SqlRepository();
                        //repo.StreamBLOBToServer("Xmas.png", fileSection.FileStream);
                    }
                    else if (contentDispo.IsFormDisposition())
                    {
                        var formSection = section.AsFormDataSection();
                        var value = await formSection.GetValueAsync();
                        valuesByKey.Add(formSection.Name, value);
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }

            return Ok();
        }

        [HttpGet("Download")]
        public IActionResult Download(bool useCustomFileResult)
        {
            var stream = new MemoryStream(_bytes);
            var contentType = "application/octet-stream";

            return useCustomFileResult
                ? (IActionResult)new CustomFileResult(stream, contentType)
                : File(stream, contentType);
        }

        [HttpGet("Download/sql/{id}")]
        public IActionResult DownloadFromSql(int id)
        {
            SqlRepository repo = new SqlRepository();
            var stream = repo.GetBinaryValue(id);
            //MemoryStream ms = new MemoryStream();
            //stream.CopyTo(ms);
            var contentType = "application/octet-stream";
            return (IActionResult)new CustomFileResult(stream, contentType);
        }

        private static string GetBoundary(string contentType)
        {
            if (contentType == null)
                throw new ArgumentNullException(nameof(contentType));

            var elements = contentType.Split(' ');
            var element = elements.First(entry => entry.StartsWith("boundary="));
            var boundary = element.Substring("boundary=".Length);

            //boundary = HeaderUtilities.RemoveQuotes(boundary);
            boundary = HeaderUtilities.RemoveQuotes(boundary).ToString();

            return boundary;
        }
    }
}
