using Microsoft.AspNetCore.Mvc;

namespace NanoBackupWebsite
{
    // Set the URL at which files can be downloaded
    [ApiController]
    [Route("api/download")]
    public class DownloadController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetFile([FromQuery] string fullPath, [FromQuery] string fileName)
        {
            // Get the Path to the File on the local machine
            string baseFolder = AppContext.BaseDirectory;
            string path = Path.Combine(baseFolder, fullPath, fileName);

            if (!System.IO.File.Exists(path))
                return NotFound();

            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            return File(fs, "application/octet-stream", fileName, true);
        }
    }
}
