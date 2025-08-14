using AutoMapper;
using BackPredictFinance.Datas.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using System.Linq.Expressions;
using BackPredictFinance.Datas.Common;
using BackPredictFinance.Common;
using BackPredictFinance.ViewModels.WebViewModels.PaginateViewModels;
using BackPredictFinance.ViewModels.CommonViewModels;

namespace BackPredictFinance.Services
{
    public class DocumentService : BaseService
    {
		private readonly DriveService _driveService;
		private readonly PathService _pathService;

		public DocumentService(
			DriveService driveService,
			PathService pathService, IServiceProvider serviceProvider) : base(serviceProvider)
        {
			_driveService = driveService;
			_pathService = pathService;
		}

		/// <summary>
		/// add new document
		/// </summary>
		/// <param name="registerViewModel"></param>
		/// <returns></returns>
		public async Task<Document?> AddDocument(DocumentViewModel documentViewModel, CancellationToken cancellationToken = default)
		{
			var newDocument = Mapper.Map<Document>(documentViewModel);
			newDocument.SetParams();

			await FinanceDbContext.Documents.AddAsync(newDocument, cancellationToken);

			await FinanceDbContext.SaveChangesAsync(cancellationToken);	

			await _driveService.AddFile(documentViewModel.File, newDocument.Id);

			return newDocument;
		}

		public async Task DeleteDocument(string documentId)
		{
			var document = await FinanceDbContext.Documents.FirstOrDefaultAsync(x => x.Id == documentId);

			FinanceDbContext.Documents.Remove(document);
			await FinanceDbContext.SaveChangesAsync();

            _driveService.DeleteFile(_pathService.GetFilePath(document.Id));

            var matchingFiles = Directory.GetFiles(_pathService.GetFolderPath())
                         .Where(f => Path.GetFileName(f).Contains(documentId) && Path.GetFileName(f).Contains("_mini"))
                         .ToList();

            if (matchingFiles.Any())
            {
                foreach (var file in matchingFiles)
                {
                    _driveService.DeleteFile(_pathService.GetFilePath(file));
                }
            }
        }


		public async Task<DocumentPaginateViewModel> GetAllDocumentByPagination(PaginateViewModel paginate)
		{
			var documentList = await FinanceDbContext.Documents.GetByPaginationAsync(paginate.PageIndex * paginate.PageSize, paginate.PageSize, paginate.SortActive, paginate.SortDirection, GetFilter(paginate.Filter));

            var documentListVm = Mapper.Map<List<DocumentViewModel>>(documentList);

			var count = await FinanceDbContext.Documents.GetTotalCountAsync();

			var documentPaginate = new DocumentPaginateViewModel(count, documentListVm);

			return documentPaginate;
		}

		private Expression<Func<Document, bool>> GetFilter(string filter)
		{
			filter = filter ?? string.Empty;
			filter = filter.ToLower().Trim();

			Expression<Func<Document, bool>> find = x =>

			filter == "" ||
			x.FileName.ToLower().Trim().Contains(filter);

			return find;
		}

        public async Task<DocumentViewModel> GetDocumentDatas(string documentId)
		{
            var document = await FinanceDbContext.Documents.FirstOrDefaultAsync(x => x.Id == documentId);
            var documentVm = Mapper.Map<DocumentViewModel>(document);
			return documentVm;
		}

		public async Task UpdateDocument(DocumentViewModel documentVm)
		{
            var document = await FinanceDbContext.Documents.FirstOrDefaultAsync(x => x.Id == documentVm.Id);
			if (document == null)
				throw new CustomException("Document not found");

            if (documentVm.File == null || documentVm.File.Length == 0)
            {
                _logger.LogInformation("Update skipped: no file provided for document {DocumentId}", documentVm.Id);
                return;
            }

            var existingFilePath = _pathService.GetFilePath(document.Id);
            try
            {
                _driveService.DeleteFile(existingFilePath);
            }
            catch (CustomException ex)
            {
                _logger.LogWarning(ex.Message);
            }

            document.FileName = documentVm.File.FileName;
            document.UpdatedAtUtc = DateTime.UtcNow;

            FinanceDbContext.Documents.Update(document);

			await FinanceDbContext.SaveChangesAsync();

            await _driveService.AddFile(documentVm.File, documentVm.Id);
        }


        public async Task<byte[]> DownloadFile(string documentId)
		{
			var document = await FinanceDbContext.Documents.FirstOrDefaultAsync(x => x.Id == documentId);
            var path = _pathService.GetFilePath(document.Id);
            byte[] bytes = await File.ReadAllBytesAsync(path);

			return bytes;
		}


		public async Task CreateMiniPicture(string logoId)
		{
			string originalFilePath = _pathService.GetFilePath(logoId);

			try
			{
				var detectedFormat = await Image.DetectFormatAsync(originalFilePath);
			}
			catch
			{
				_logger.LogInformation($"The file \"{originalFilePath}\" is not a supported image format. Must be \".jpg\", \".jpeg\", or \".png\".");
				return;
			}

			// Load the image
			using (Image image = await Image.LoadAsync(originalFilePath))
			{
				// Calculate the new width to maintain the aspect ratio
				var originalWidth = image.Width;
				var originalHeight = image.Height;
				var newHeight = 400; // Maximum height
				var newWidth = newHeight * originalWidth / originalHeight;

				// Resize the image to the new width and height, maintaining the aspect ratio
				image.Mutate(x => x.Resize(newWidth, newHeight));

				// Detect the image format
				var detectedFormat = image.Metadata.DecodedImageFormat ?? await Image.DetectFormatAsync(originalFilePath);

				// Detect the image format (JPEG or PNG) to set the encoder accordingly
				IImageEncoder encoder = detectedFormat switch
				{
					JpegFormat => new JpegEncoder { Quality = 30 },
					PngFormat => new PngEncoder { CompressionLevel = PngCompressionLevel.Level7 },
					_ => null
				};

				if (encoder == null)
				{
					_logger.LogInformation($"The file \"{originalFilePath}\" with detected format = {detectedFormat?.Name} cannot be compressed. Must be \".jpg\", \".jpeg\", or \".png\" type");
					return;
				}

				// Use the detected format to create the mini picture name
				var miniPictureName = logoId + "_mini";

				// Get the full path for the mini picture
				var miniPictureFullPath = _pathService.GetFilePath(miniPictureName);

				// Save the mini image using the detected encoder
				await image.SaveAsync(miniPictureFullPath, encoder);
			}


		}



    }


}
