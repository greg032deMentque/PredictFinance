using BackPredictFinance.Services.ClientFinanceServices;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.Contact;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackPredictFinance.API.Controllers.ClientFinance
{
    [Authorize(Policy = "Bearer")]
    [Route("api/ClientFinance")]
    [ApiController]
    public class ClientFinanceContactController(IClientFinanceContactService contactService) : ControllerBase
    {
        [HttpPost("contact")]
        public async Task<IActionResult> Contact([FromBody] ContactSupportRequestViewModel model, CancellationToken ct)
        {
            await contactService.SendSupportMessageAsync(model, ct);
            return NoContent();
        }
    }
}
