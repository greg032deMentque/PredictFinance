using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.Services.ClientFinanceServices.Alerts;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Alerts;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Contact;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.ClientFinance
{
    [Authorize(Policy = "Bearer")]
    [Route("api/ClientFinance")]
    [ApiController]
    public class ClientFinanceContactController(
        IClientFinanceContactService contactService,
        IClientAlertService clientAlertService) : ControllerBase
    {
        [HttpPost("contact")]
        public async Task<IActionResult> Contact([FromBody] ContactSupportRequestViewModel model, CancellationToken ct)
        {
            await contactService.SendSupportMessageAsync(model, ct);
            return NoContent();
        }

        [HttpPost("alerts")]
        public async Task<IActionResult> CreateAlert([FromBody] CreateClientAlertRequestViewModel model, CancellationToken ct)
        {
            return Ok(await clientAlertService.CreateAlertAsync(model, ct));
        }
    }
}
