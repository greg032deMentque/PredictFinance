using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace BackPredictFinance.Services
{
	public interface ILogService
	{
		public void LogError(string message, [CallerMemberName] string functionName = "");
		public void LogError(string customMessage, Exception ex, [CallerMemberName] string functionName = "");
		public void LogError(Exception ex, [CallerMemberName] string functionName = "");
		public void LogInformation(string message, [CallerMemberName] string functionName = "");
		public void LogWarning(string message, [CallerMemberName] string functionName = "");
        public void LogWarning(string customMessage, Exception ex, [CallerMemberName] string functionName = "");
		public void LogDebug(string message, [CallerMemberName] string functionName = "");

    }
	public class LogService : ILogService
	{
		protected string _currentUserId;

		// c'est déprécié mais je n'ai pas trouvé d'autre solution
		public readonly HttpContext _context;

		public IConfiguration _configuration { get; set; }

		public LogService(IHttpContextAccessor httpContextAccessor)
		{
			_context = httpContextAccessor.HttpContext;

			// Get connected user
			if (_context?.User.FindFirst(ClaimTypes.Sid) != null)
			{
				_currentUserId = Convert.ToString(_context.User.FindFirst(ClaimTypes.Sid).Value);
			}
			else
			{
				_currentUserId = "null";
			}
		}


		public void LogError(string message, [CallerMemberName] string functionName = "")
		{
			Log.Error($"User id: {_currentUserId}. Function: {functionName}. Message: {message}");
		}
		public void LogError(string customMessage, Exception ex, [CallerMemberName] string functionName = "")
		{
			Log.Error(
				$"User id: {_currentUserId}. " +
				$"Function: {functionName}. " +
				$"Custom message: {customMessage}. " +
				$"Exception message: {ex.Message}. " +
				$"Exception StackTrace: {ex.StackTrace}");
		}
		public void LogError(Exception ex, [CallerMemberName] string functionName = "")
		{
			Log.Error(
				$"User id: {_currentUserId}. " +
				$"Function: {functionName}. " +
				$"Message: {ex.Message}. " +
				$"StackTrace: {ex.StackTrace}");
		}

		public void LogWarning(string message, [CallerMemberName] string functionName = "")
		{
			Log.Warning($"User id: {_currentUserId}. Function: {functionName}. Message: {message}");
		}
        public void LogWarning(string customMessage, Exception ex, [CallerMemberName] string functionName = "")
        {
            Log.Warning(
                $"User id: {_currentUserId}. " +
                $"Function: {functionName}. " +
                $"Custom message: {customMessage}. " +
                $"Exception message: {ex.Message}. " +
                $"Exception StackTrace: {ex.StackTrace}");
        }

        public void LogInformation(string message, [CallerMemberName] string functionName = "")
		{
			Log.Information($"User id: {_currentUserId}. Function: {functionName}. Message: {message}");
		}

		public void LogDebug(string message, [CallerMemberName] string functionName = "")
		{
			Log.Debug($"User id: {_currentUserId}. Function: {functionName}. Message: {message}");
		}
	}

}
