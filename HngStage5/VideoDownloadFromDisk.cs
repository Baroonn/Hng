using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HngStage5
{

    public static class VideoDownloadFromDisk
    {


        [FunctionName("VideoDownloadFromDisk")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{filename}")] HttpRequest req,
            ILogger log, string filename)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var file = Directory.GetFiles(@$"/fx-files")
                .FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(filename));
            if (file == null)
            {
                return new NotFoundObjectResult(new
                {
                    Status = "fail",
                    Data = new { Message = "File not found" }
                });
            }

            var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite);
            string mimeType = Helpers.GetMimeType(file);

            stream.Position = 0;
            return new FileStreamResult(stream, mimeType);
        }
    }
}
