using Microsoft.AspNetCore.StaticFiles;

namespace HngStage5
{
    public static class Helpers
    {
        public static string GetMimeType(string filename)
        {
            var provider = new FileExtensionContentTypeProvider();
            string contentType;
            if (!provider.TryGetContentType(filename, out contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }
    }
}
