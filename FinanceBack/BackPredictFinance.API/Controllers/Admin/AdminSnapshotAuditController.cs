using BackPredictFinance.Services.AdminGovernance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminSnapshotAuditController : ControllerBase
    {
        private readonly IAdminSnapshotAuditService _adminSnapshotAuditService;

        public AdminSnapshotAuditController(IAdminSnapshotAuditService adminSnapshotAuditService)
        {
            _adminSnapshotAuditService = adminSnapshotAuditService;
        }

        [HttpGet("snapshot-audit")]
        public async Task<IActionResult> GetRecent([FromQuery] int take = 50, CancellationToken ct = default)
        {
            return Ok(await _adminSnapshotAuditService.GetRecentAsync(take, ct));
        }

        [HttpGet("snapshot-audit/{analysisRunId}")]
        public async Task<IActionResult> GetDetail([FromRoute] string analysisRunId, CancellationToken ct)
        {
            return Ok(await _adminSnapshotAuditService.GetDetailAsync(analysisRunId, ct));
        }

        [HttpGet("snapshot-audit/compare")]
        public async Task<IActionResult> Compare([FromQuery] string leftId, [FromQuery] string rightId, CancellationToken ct)
        {
            return Ok(await _adminSnapshotAuditService.CompareAsync(leftId, rightId, ct));
        }
    }
}
