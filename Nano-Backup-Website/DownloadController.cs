using Microsoft.AspNetCore.Mvc;

namespace NanoBackupWebsite
{
    // Set the URL at which files can be downloaded
    [ApiController]
    [Route("api/download")]
    public class DownloadController : ControllerBase, IDisposable
    {
        Stream? Stream;

        [HttpGet]
        public IActionResult GetFile([FromQuery] string id, [FromQuery] string fileName, [FromQuery] string returnUrl)
        {
            Console.WriteLine($"Downloading File : {fileName}");

            Stream = new SQLClient().GetFileStream(int.Parse(id));

            if (Stream == null)
                return Redirect($"{returnUrl}?error=NotFound");

            return File(Stream, "application/octet-stream", fileName, true);
        }

        public void Dispose()
        {
            Stream?.Dispose();

            GC.Collect(2, GCCollectionMode.Aggressive, true);
        }
    }
}