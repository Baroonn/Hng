using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace HngStage5
{
    public static class TranscriptDownload
    {
        [FunctionName("TranscriptDownload")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{filename}/transcript")] HttpRequest req,
            ILogger log, string filename)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var file = Directory.GetFiles(@$"Z:/")
                .FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(filename));

            if (file == null)
            {
                return new NotFoundObjectResult(new
                {
                    Status = "fail",
                    Data = new { Message = "File not found" }
                });
            }
            var fileTranscript = Directory.GetFiles(@$"Z:/transcripts")
                .FirstOrDefault(x => Path.GetFileNameWithoutExtension(x).Equals(filename));
            if (fileTranscript != null)
            {
                var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite);
                string mimeType = Helpers.GetMimeType(file);

                stream.Position = 0;
                return new FileStreamResult(stream, mimeType);
            }
            //await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
            var FFmpegpath = "Z:/FFmpeg/bin";
            FFmpeg.SetExecutablesPath(FFmpegpath);
            var a = Directory.GetCurrentDirectory();
            string input = @$"Z:/{Path.GetFileName(file)}";
            string output = @$"Z:/" + Path.ChangeExtension(Path.GetRandomFileName(), "mp3");
            //Extract audio from video file
            var result = await FFmpeg.Conversions.FromSnippet.ExtractAudio(input, output);

            await result.Start();
            return new OkResult();
        }
    }
}
