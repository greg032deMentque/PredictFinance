using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BackPredictFinance.Services.TwelveDataServices.Models
{
    /// <summary>
    /// Modèle pour la réponse de l'API Twelve Data sur les séries temporelles.
    /// Contient les valeurs retournées et le statut de la requête.
    /// </summary>
    public class TimeSeriesResponse
    {
        /// <summary>
        /// Liste des points de la série temporelle (valeurs OHLCV).
        /// </summary>
        [JsonPropertyName("values")]
        public List<TimeSeriesPoint> Values { get; set; } = new List<TimeSeriesPoint>();

        /// <summary>
        /// Statut de la réponse ("ok" si la requête a réussi, sinon "error").
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Métadonnées associées à la réponse (intervalle, symbole, timezone, etc.).
        /// </summary>
        [JsonPropertyName("meta")]
        public object? Meta { get; set; }
    }
}
