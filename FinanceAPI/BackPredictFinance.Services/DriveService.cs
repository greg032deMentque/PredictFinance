using BackPredictFinance.Common;
using Microsoft.AspNetCore.Http;

namespace BackPredictFinance.Services
{
	public class DriveService
    {
        private readonly PathService _pathService;
        private readonly ILogService _logger;
        public DriveService(PathService pathService, ILogService logService)
        {
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _logger = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public async Task AddFile(IFormFile file, string fileName)
        {
            if (file == null || file.Length == 0)
            {
                throw new CustomException("The provided file is either null or empty.");
            }

            try
            {
                            
                var filePath = _pathService.GetFilePath(fileName);

                _logger.LogInformation("Saving file to path: {FilePath}", filePath);
                await SaveFileAsync(file, filePath);

                _logger.LogInformation("File {FileName} successfully added.", file.FileName);
            }
            catch (Exception ex)
            {
                throw new CustomException(ex.Message);
            }
        }

        public async Task SaveFileAsync(IFormFile file, string filePath)
        {
            _logger.LogInformation("Starting to save file: {FilePath}", filePath);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("File saved successfully: {FilePath}", filePath);
        }

        public void DeleteFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                throw new CustomException($"The file at {filePath} does not exist.");
            }

            try
            {
                _logger.LogInformation("Deleting file at path: {FilePath}", filePath);
                File.Delete(filePath);
                _logger.LogInformation("File at path {FilePath} successfully deleted.", filePath);
            }
            catch (Exception ex)
            {
                throw new CustomException(ex.Message);
            }
        }
    }
}
