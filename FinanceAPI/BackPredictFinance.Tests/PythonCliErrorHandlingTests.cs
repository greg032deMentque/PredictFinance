using BackPredictFinance.Common;
using BackPredictFinance.Services.PythonServices;
using BackPredictFinance.Services.PythonServices.Models;
using System.Net;

namespace BackPredictFinance.Tests
{
    public class PythonCliErrorHandlingTests
    {
        [Fact]
        public void TryParseEnvelope_Parses_JsonPayload()
        {
            var payload = """
                {
                  "schema_version": "1.0",
                  "source": "cli",
                  "operation": "predict",
                  "error_code": "artifact_missing",
                  "error_type": "FileNotFoundError",
                  "message": "Model file not found: artifacts/double_top/model.joblib",
                  "user_message": "Le modele IA est indisponible pour le moment.",
                  "ticker": "AAPL",
                  "pattern": "DOUBLE_TOP",
                  "details": { "missing_file": "model.joblib" },
                  "logged_at_utc": "2026-03-20T10:00:00Z"
                }
                """;

            var success = PythonCliErrorHandling.TryParseEnvelope(payload, out var envelope);

            Assert.True(success);
            Assert.Equal("artifact_missing", envelope.ErrorCode);
            Assert.Equal("predict", envelope.Operation);
            Assert.Equal("AAPL", envelope.Ticker);
            Assert.Equal("DOUBLE_TOP", envelope.Pattern);
            Assert.Equal("Le modele IA est indisponible pour le moment.", envelope.UserMessage);
        }

        [Theory]
        [InlineData("invalid_input", HttpStatusCode.BadRequest)]
        [InlineData("data_unavailable", (HttpStatusCode)422)]
        [InlineData("artifact_missing", HttpStatusCode.ServiceUnavailable)]
        [InlineData("invalid_output", HttpStatusCode.BadGateway)]
        [InlineData("unexpected_error", HttpStatusCode.InternalServerError)]
        public void MapStatusCode_Returns_ExpectedStatus(string errorCode, HttpStatusCode expectedStatus)
        {
            var statusCode = PythonCliErrorHandling.MapStatusCode(errorCode);

            Assert.Equal(expectedStatus, statusCode);
        }

        [Fact]
        public void CreateCustomException_AttachesEnvelope_AndUsesFrontMessage()
        {
            var envelope = new PythonCliErrorEnvelope
            {
                SchemaVersion = "1.0",
                Source = "cli",
                Operation = "simulate",
                ErrorCode = "invalid_input",
                ErrorType = "ValueError",
                Message = "investment_amount must be strictly positive",
                UserMessage = "La requête envoyée au moteur IA est invalide.",
                Ticker = "AAPL",
                Pattern = "DOUBLE_TOP",
                LoggedAtUtc = DateTime.UtcNow
            };

            var exception = PythonCliErrorHandling.CreateCustomException("simulate", "AAPL", "DOUBLE_TOP", envelope);

            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
            Assert.Equal("La requête envoyée au moteur IA est invalide.", exception.FrontMessage);
            Assert.Contains("errorCode=invalid_input", exception.Message, StringComparison.Ordinal);
            Assert.NotNull(PythonCliErrorHandling.TryGetSerializedEnvelope(exception));
        }
    }
}
