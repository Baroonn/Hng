using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HngStage5
{
    public static class VideoUploadToDisk
    {
        [FunctionName("VideoUploadToDisk")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/upload")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var uploadFile = req.Form.Files[0];

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(uploadFile.FileName)
                + "_"
                + DateTime.UtcNow.ToString().Split(" ")[0].Replace(@"/", "_");
            var fileName = fileNameWithoutExtension
                + Path.GetExtension(uploadFile.FileName);
            var files = Directory.GetFiles(@$"/fx-files").Where(x => x.Contains(fileNameWithoutExtension)).ToList();
            if (files.Count > 0)
            {
                return new BadRequestObjectResult(new
                {
                    Status = "fail",
                    Data = new { Message = "Filename already exists" }
                });
            }
            using (Stream stream = uploadFile.OpenReadStream())
            {
                FileStream fileStream = File.Create(@$"/fx-files/{fileName}", (int)stream.Length);
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, bytesInStream.Length);
                fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                fileStream.Close();

                return new CreatedAtRouteResult($"api/stream/{fileName}", new
                {
                    Status = "success",
                    Data = new { Name = fileName, Url = $"api/stream/{fileName}" }
                });
            }
        }
    }
}
