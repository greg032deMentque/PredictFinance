using BackPredictFinance.Common;
using BackPredictFinance.Services.PythonServices.Models;
using System.Net;
using System.Text.Json;

namespace BackPredictFinance.Services.PythonServices
{
    internal static class PythonCliErrorHandling
    {
        internal const string EnvelopeExceptionDataKey = "PythonCliErrorEnvelopeJson";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        internal static bool TryParseEnvelope(string? rawPayload, out PythonCliErrorEnvelope envelope)
        {
            envelope = new PythonCliErrorEnvelope();

            if (string.IsNullOrWhiteSpace(rawPayload))
            {
                return false;
            }

            var candidates = rawPayload
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Reverse()
                .Prepend(rawPayload.Trim());

            foreach (var candidate in candidates)
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<PythonCliErrorEnvelope>(candidate, SerializerOptions);
                    if (parsed == null || string.IsNullOrWhiteSpace(parsed.ErrorCode))
                    {
                        continue;
                    }

                    envelope = parsed;
                    envelope.UserMessage = string.IsNullOrWhiteSpace(envelope.UserMessage)
                        ? MapUserMessage(envelope.ErrorCode)
                        : envelope.UserMessage.Trim();
                    envelope.Message = string.IsNullOrWhiteSpace(envelope.Message)
                        ? "Python CLI failed without a technical message."
                        : envelope.Message.Trim();
                    return true;
                }
                catch (JsonException)
                {
                }
            }

            return false;
        }

        internal static PythonCliErrorEnvelope BuildFallbackEnvelope(
            string operation,
            string? symbol,
            string? pattern,
            string errorCode,
            string technicalMessage,
            string? userMessage = null,
            Dictionary<string, string?>? details = null,
            string source = "backpredictfinance.api")
        {
            return new PythonCliErrorEnvelope
            {
                SchemaVersion = "1.0",
                Source = source,
                Operation = operation,
                ErrorCode = errorCode,
                ErrorType = "DotNetFallback",
                Message = Truncate(technicalMessage, 2048),
                UserMessage = string.IsNullOrWhiteSpace(userMessage) ? MapUserMessage(errorCode) : userMessage.Trim(),
                Ticker = Normalize(symbol),
                Pattern = Normalize(pattern),
                Details = details,
                LoggedAtUtc = DateTime.UtcNow
            };
        }

        internal static HttpStatusCode MapStatusCode(string? errorCode)
        {
            return (errorCode ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "invalid_input" => HttpStatusCode.BadRequest,
                "data_unavailable" => (HttpStatusCode)422,
                "artifact_missing" => HttpStatusCode.ServiceUnavailable,
                "invalid_output" => HttpStatusCode.BadGateway,
                _ => HttpStatusCode.InternalServerError
            };
        }

        internal static string MapUserMessage(string? errorCode)
        {
            return (errorCode ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "invalid_input" => "La requête envoyée au moteur IA est invalide.",
                "data_unavailable" => "Les données de marché nécessaires sont indisponibles pour le moment.",
                "artifact_missing" => "Le modèle IA est indisponible pour le moment.",
                "invalid_output" => "Le résultat du moteur IA est invalide.",
                _ => "Une erreur interne est survenue côté IA."
            };
        }

        internal static CustomException CreateCustomException(
            string operation,
            string? symbol,
            string? pattern,
            PythonCliErrorEnvelope envelope,
            HttpStatusCode? overrideStatusCode = null,
            string? overrideFrontMessage = null)
        {
            var statusCode = overrideStatusCode ?? MapStatusCode(envelope.ErrorCode);
            var frontMessage = string.IsNullOrWhiteSpace(overrideFrontMessage)
                ? (string.IsNullOrWhiteSpace(envelope.UserMessage) ? MapUserMessage(envelope.ErrorCode) : envelope.UserMessage.Trim())
                : overrideFrontMessage.Trim();

            var logMessage =
                $"Python CLI {operation} failed for symbol={Normalize(symbol) ?? "<none>"} pattern={Normalize(pattern) ?? "<none>"} errorCode={envelope.ErrorCode} errorType={envelope.ErrorType} message={Truncate(envelope.Message, 1024)}";

            var customException = new CustomException(logMessage, frontMessage, statusCode: statusCode);
            AttachEnvelope(customException, envelope);
            return customException;
        }

        internal static void AttachEnvelope(Exception exception, PythonCliErrorEnvelope envelope)
        {
            exception.Data[EnvelopeExceptionDataKey] = SerializeEnvelope(envelope);
        }

        internal static string? TryGetSerializedEnvelope(Exception exception)
        {
            return exception.Data.Contains(EnvelopeExceptionDataKey)
                ? exception.Data[EnvelopeExceptionDataKey] as string
                : null;
        }

        internal static PythonCliErrorEnvelope GetOrBuildEnvelope(Exception exception, string operation, string? symbol, string? pattern)
        {
            if (TryGetSerializedEnvelope(exception) is { Length: > 0 } serialized &&
                TryParseEnvelope(serialized, out var parsedEnvelope))
            {
                return parsedEnvelope;
            }

            if (exception is CustomException customException && !string.IsNullOrWhiteSpace(customException.FrontMessage))
            {
                return BuildFallbackEnvelope(
                    operation,
                    symbol,
                    pattern,
                    "unexpected_error",
                    exception.Message,
                    customException.FrontMessage);
            }

            return BuildFallbackEnvelope(operation, symbol, pattern, "unexpected_error", exception.Message);
        }

        internal static string SerializeEnvelope(PythonCliErrorEnvelope envelope)
        {
            return JsonSerializer.Serialize(envelope);
        }

        internal static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
