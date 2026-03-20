using System.Net;
using System.Runtime.CompilerServices;

namespace BackPredictFinance.Common
{
    public class CustomException : Exception
	{
		public List<string> ErrorMessages { get; } = [];

		public HttpStatusCode StatusCode { get; }
		public string FunctionName { get; set; }
		public string FrontMessage { get; set; } = string.Empty;

		public CustomException(
			string logMessage,
			string? frontMessage = null,
			List<string>? errors = default,
			HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
			[CallerMemberName] string functionName = "")
			: base(logMessage)
		{
			ErrorMessages = errors ?? [];
			StatusCode = statusCode;
			FunctionName = functionName;
			FrontMessage = frontMessage ?? string.Empty;
		}
	}
}
