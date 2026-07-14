using BackPredictFinance.Common.enums;
using BackPredictFinance.Services.AdminGovernance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    [Route("api/admin")]
    [ApiController]
    public sealed class AdminKpiController : ControllerBase
    {
        private readonly IAdminKpiService _adminKpiService;
        private readonly IAdminSignalQualityService  _adminSignalQualityService;

        public AdminKpiController(IAdminKpiService adminKpiService, IAdminSignalQualityService  adminSignalQualityService)
        {
            _adminKpiService = adminKpiService;
            _adminSignalQualityService = adminSignalQualityService;
        }

        [HttpGet("kpi/engagement")]
        public async Task<IActionResult> GetEngagement([FromQuery] string window = "D30", CancellationToken ct = default)
        {
            if (!TryParseEngagementWindow(window, out var kpiWindow))
                return BadRequest("window doit être D7, D30 ou D90.");

            return Ok(await _adminKpiService.GetEngagementKpiAsync(kpiWindow, ct));
        }

        [HttpGet("kpi/engagement/export")]
        public async Task<IActionResult> ExportEngagement([FromQuery] string window = "D30", CancellationToken ct = default)
        {
            if (!TryParseEngagementWindow(window, out var kpiWindow))
                return BadRequest("window doit être D7, D30 ou D90.");

            var kpi = await _adminKpiService.GetEngagementKpiAsync(kpiWindow, ct);
            var csv = AdminKpiCsvWriter.BuildEngagementCsv(kpi);
            return File(csv, "text/csv", $"kpi-engagement-{window}.csv");
        }

        private static bool TryParseEngagementWindow(string value, out KpiWindow result)
        {
            switch (value)
            {
                case "D7": result = KpiWindow.D7; return true;
                case "D30": result = KpiWindow.D30; return true;
                case "D90": result = KpiWindow.D90; return true;
                default: result = KpiWindow.D30; return false;
            }
        }

        [HttpGet("kpi/signal-quality")]
        public async Task<IActionResult> GetSignalQuality(
            [FromQuery] string window = "D30",
            [FromQuery] string? policyVersion = null,
            CancellationToken ct = default)
        {
            if (!TryParseSignalQualityWindow(window, out var days))
                return BadRequest("window doit être D30 ou D90.");
            return Ok(await _adminSignalQualityService.GetSignalQualityKpiAsync(days, policyVersion, ct));
        }

        [HttpGet("kpi/signal-quality/export")]
        public async Task<IActionResult> ExportSignalQuality(
            [FromQuery] string window = "D30",
            [FromQuery] string? policyVersion = null,
            CancellationToken ct = default)
        {
            if (!TryParseSignalQualityWindow(window, out var days))
                return BadRequest("window doit être D30 ou D90.");

            var kpi = await _adminSignalQualityService.GetSignalQualityKpiAsync(days, policyVersion, ct);
            var csv = AdminKpiCsvWriter.BuildSignalQualityCsv(kpi);
            return File(csv, "text/csv", $"kpi-signal-quality-{window}.csv");
        }

        private static bool TryParseSignalQualityWindow(string value, out int days)
        {
            switch (value)
            {
                case "D30": days = 30; return true;
                case "D90": days = 90; return true;
                default: days = 30; return false;
            }
        }
    }
}
