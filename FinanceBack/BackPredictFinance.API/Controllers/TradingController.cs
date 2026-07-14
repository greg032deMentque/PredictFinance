using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace BackPredictFinance.API.Controllers
{
    [Authorize(Policy = "Bearer")]
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class TradingController : ControllerBase
    {
        [HttpPost("predict")]
        public IActionResult Predict()
            => BuildRetiredEndpointResult();

        [HttpGet("predict/{symbol}")]
        public IActionResult PredictBySymbol([FromRoute] string symbol)
            => BuildRetiredEndpointResult();

        private ObjectResult BuildRetiredEndpointResult()
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status410Gone,
                Title = "Endpoint retired",
                Detail = "L'ancien endpoint de prediction directe n'appartient plus a la surface V1. Utilisez l'analyse client API-owned."
            };

            return StatusCode(
                StatusCodes.Status410Gone,
                problemDetails);
        }
    }
}
