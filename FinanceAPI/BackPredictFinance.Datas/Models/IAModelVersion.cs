using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackPredictFinance.Datas.Models
{
    /// <summary>
    /// Permet de tracer quelle version de modèle IA a généré chaque prédiction
    /// </summary>
    public class IAModelVersion : AuditableEntityBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Numéro ou hash de la version du modèle
        /// </summary>
        [Required]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Date de déploiement / entraînement du modèle
        /// </summary>
        public DateTime DeployedAtUtc { get; set; }

        /// <summary>
        /// Commentaire ou méta-données
        /// </summary>
        public string? Description { get; set; }
    }
}
