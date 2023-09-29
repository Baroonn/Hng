using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;

namespace HngStage5
{
    public static class VideoUpload
    {
        [FunctionName("VideoUpload")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api")] HttpRequest req,
            [Blob($"blobcontainer", Connection = "StorageConnectionString")] BlobContainerClient outputContainer,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            await outputContainer.CreateIfNotExistsAsync();

            var file = req.Form.Files[0];

            var blobName = Path.GetFileNameWithoutExtension(file.FileName)
                + "_"
                + DateTime.UtcNow.ToString().Split(" ")[0].Replace(@"/", "_")
                + Path.GetExtension(file.FileName);

            var blob = outputContainer.GetBlobClient(blobName);

            if (await blob.ExistsAsync())
            {
                return new BadRequestObjectResult(new
                {
                    Status = "fail",
                    Data = new { Message = "Filename already exists" }
                });
            }
            try
            {
                await blob.UploadAsync(file.OpenReadStream());
            }
            catch (Exception)
            {
                return new InternalServerErrorResult();
            }
            BlobProperties properties = await blob.GetPropertiesAsync();

            BlobHttpHeaders headers = new BlobHttpHeaders
            {
                ContentType = file.ContentType,
                CacheControl = properties.CacheControl,
                ContentDisposition = file.ContentDisposition,
                ContentEncoding = properties.ContentEncoding,
                ContentHash = properties.ContentHash
            };

            await blob.SetHttpHeadersAsync(headers);
            return new OkObjectResult(new
            {
                Status = "success",
                Data = new { Name = blobName, Url = blob.Uri }
            });
        }
    }
}
