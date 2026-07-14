using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.Notifications;
using BackPredictFinance.ViewModels.NotificationViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers
{
    [Authorize(Policy = "Bearer")]
    [Route("api/[controller]")]
    [ApiController]
    public sealed class NotificationsController : ControllerBase
    {
        private readonly INotificationCenterService _notificationCenterService;

        public NotificationsController(INotificationCenterService notificationCenterService)
        {
            _notificationCenterService = notificationCenterService;
        }

        [HttpGet("GetList")]
        public async Task<IActionResult> GetList([FromQuery] NotificationCategoryEnum? category, [FromQuery] NotificationStatusEnum? status, [FromQuery] int take = 50, CancellationToken ct = default)
        {
            var payload = await _notificationCenterService.GetListAsync(category, status, take, ct);
            return Ok(payload);
        }

        [HttpPost("MarkAsRead")]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkNotificationAsReadRequestViewModel request, CancellationToken ct = default)
        {
            var payload = await _notificationCenterService.MarkAsReadAsync(request.NotificationId, ct);
            return Ok(payload);
        }
    }
}
