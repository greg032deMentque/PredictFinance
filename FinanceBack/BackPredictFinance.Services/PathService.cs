using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace BackPredictFinance.Services
{
    /// <summary>
    /// Construit les chemins de stockage et les URLs publiques des fichiers uploadés.
    /// </summary>
    public interface IPathService
    {
        /// <summary>
        /// Retourne le chemin physique complet d'un fichier uploadé.
        /// </summary>
        string GetFilePath(string fileName);
        /// <summary>
        /// Retourne le chemin physique d'une miniature associée à un fichier.
        /// </summary>
        string GetMiniPicturePath(string fileName);
        /// <summary>
        /// Retourne le dossier physique racine des uploads.
        /// </summary>
        string GetFolderPath();
        /// <summary>
        /// Construit l'URL publique d'accès à un document uploadé.
        /// </summary>
        string GetPublicUrl(string docId, string originalFileName);
    }

    /// <summary>
    /// Implémente la résolution des chemins et URLs des fichiers publics.
    /// </summary>
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
