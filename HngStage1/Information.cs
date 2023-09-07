using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

namespace HngStage1
{
    public static class Information
    {
        [FunctionName("Information")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["slack_name"];
            string track = req.Query["track"];
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(track))
            {
                return new BadRequestErrorMessageResult("slack_name or track was not provided");
            }

            DateTime dateTime = DateTime.UtcNow;

            dateTime = new DateTime(
                dateTime.Ticks - (dateTime.Ticks % TimeSpan.TicksPerSecond),
                dateTime.Kind
                );

            return new OkObjectResult(new
            {
                slack_name = name,
                current_day = DateTime.UtcNow.DayOfWeek.ToString(),
                utc_time = dateTime,
                track,
                github_file_url = "https://www.github.com/Baroonn/Hng/HngStage1/Information.cs",
                github_repo_url = "https://www.github.com/Baroonn/Hng",
                status_code = HttpStatusCode.OK
            });
        }
    }
}
