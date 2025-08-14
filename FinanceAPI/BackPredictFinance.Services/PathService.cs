using Microsoft.Extensions.Configuration;

namespace BackPredictFinance.Services
{
    public class PathService
    {
        private ILogService _logger;

        /// <summary>
        /// path for save the <see cref="Multimedia"/>
        /// </summary>
        public string _mediaSavePath;
        public string _logoSavePath;

        IConfiguration _configuration;

        public PathService(ILogService logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _mediaSavePath = Path.GetFullPath(_configuration.GetSection("Uploads").Value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetFilePath(string fileName)
        {
            return Path.GetFullPath(Path.Combine(_mediaSavePath, fileName));
        }

		public string GetMiniPicturePath(string fileName)
		{
			return Path.GetFullPath(Path.Combine(_mediaSavePath, fileName + "_mini"));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="folderName"></param>
		/// <returns></returns>
		public string GetFolderPath()
        {
            return Path.GetFullPath(_mediaSavePath);
        }


        public string GetPublicUrl(string fileName)
        {
            var baseUrl = _configuration["PublicBaseUrl"];
            return $"{baseUrl}/uploads/{fileName}";
        }
    }
}
