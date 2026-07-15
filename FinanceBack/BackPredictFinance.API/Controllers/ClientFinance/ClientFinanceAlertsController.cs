using BackPredictFinance.Services.ClientFinanceServices.Alerts;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.ClientFinance
{
    [Authorize(Policy = "Bearer")]
    [Route("api/ClientFinance")]
    [ApiController]
    public class ClientFinanceAlertsController(IClientAlertService clientAlertService) : ControllerBase
    {
        [HttpPost("alerts")]
        public async Task<IActionResult> CreateAlert([FromBody] CreateClientAlertRequestViewModel model, CancellationToken ct)
        {
            return Ok(await clientAlertService.CreateAlertAsync(model, ct));
        }
    }
}
