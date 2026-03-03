using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace BackPredictFinance.Services
{
    public interface IPathService
    {
        string GetFilePath(string fileName);
        string GetMiniPicturePath(string fileName);
        string GetFolderPath();
        string GetPublicUrl(string docId, string originalFileName);
    }

    public class PathService : IPathService
    {
        private readonly string _uploadsPath;
        private readonly IHttpContextAccessor _http;

        public PathService(IWebHostEnvironment env, IHttpContextAccessor http)
        {
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            _uploadsPath = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(_uploadsPath);
            _http = http;
        }

        public string GetFilePath(string fileName)
        {
            return Path.Combine(_uploadsPath, fileName);
        }

        public string GetMiniPicturePath(string fileName)
        {
            return Path.Combine(_uploadsPath, fileName + "_mini");
        }

        public string GetFolderPath()
        {
            return _uploadsPath;
        }

        public string GetPublicUrl(string docId, string originalFileName)
        {
            var fileName = docId;
            if (!Path.HasExtension(fileName))
            {
                var extension = Path.GetExtension(originalFileName);
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    fileName += extension;
                }
            }

            return GetUploadsUrl(fileName);
        }

        private string GetUploadsUrl(string fileName)
        {
            var context = _http.HttpContext ?? throw new InvalidOperationException("No HttpContext available");
            return $"{context.Request.Scheme}://{context.Request.Host}/uploads/{fileName}";
        }
    }
}
