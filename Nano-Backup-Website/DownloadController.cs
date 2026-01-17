using Microsoft.AspNetCore.Mvc;

namespace NanoBackupWebsite
{
    // Set the URL at which files can be downloaded
    [ApiController]
    [Route("api/download")]
    public class DownloadController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetFile([FromQuery] string id, [FromQuery] string fileName, [FromQuery] string returnUrl)
        {
            // Get the Path to the File on the local machine
            //string baseFolder = AppContext.BaseDirectory;

           // if (!System.IO.File.Exists(path))
            //    return Redirect($"{returnUrl}?error=NotFound");

            Console.WriteLine($"Downloading File : {fileName}");

            Stream stream = new SQLClient().GetFileStream(int.Parse(id));

            if (stream == null)
                return Redirect($"{returnUrl}?error=NotFound");

            return File(stream, "application/octet-stream", fileName, true);
        }
    }
}