using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackPredictFinance.Services.TwelveDataServices.Models
{
    /// <summary>
    /// Représente un point de donnée dans la série chronologique des prix pour un symbole donné.
    /// </summary>
    public class TimeSeriesPoint
    {
        /// <summary>
        /// Timestamp du point de donnée (UTC).
        /// </summary>
        public DateTime Datetime { get; set; }

        /// <summary>
        /// Prix d'ouverture de la période.
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// Prix maximal atteint durant la période.
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Prix minimal atteint durant la période.
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// Prix de clôture de la période.
        /// </summary>
        public decimal Close { get; set; }

        /// <summary>
        /// Volume total échangé durant la période.
        /// </summary>
        public long Volume { get; set; }
    }
}
