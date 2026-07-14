using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    /// <summary>
    /// Fournit les libellés FR gouvernés et versionnés du bloc d'accompagnement (confiance expliquée, plan d'action).
    /// Source unique de wording backend ; aucun texte libre ne doit être inventé côté front (RM-17).
    /// </summary>
    public interface IAnalysisAccompanimentWordingProvider
    {
        /// <summary>
        /// Version de politique des libellés gouvernés d'accompagnement (traçabilité/audit).
        /// </summary>
        string PolicyVersion { get; }

        /// <summary>
        /// Retourne le libellé FR gouverné d'un critère de confiance à partir de son code stable.
        /// </summary>
        string GetConfidenceCriterionLabel(string criterionCode);

        /// <summary>
        /// Retourne le libellé FR gouverné d'une étape de plan d'action à partir de son type.
        /// </summary>
        string GetActionStepLabel(ActionStepKind kind);
    }

    /// <summary>
    /// Implémente une source de wording gouvernée en dur et versionnée pour la V1.
    /// À migrer ultérieurement vers le dictionnaire de paramètres / wording-versions gouverné côté admin.
    /// </summary>
    public sealed class AnalysisAccompanimentWordingProvider : IAnalysisAccompanimentWordingProvider
    {
        public string PolicyVersion => "accompaniment-fr@v1";

        public string GetConfidenceCriterionLabel(string criterionCode)
        {
            return criterionCode switch
            {
                ConfidenceCriterionCodes.StructureCompatible => "Structure du pattern compatible",
                ConfidenceCriterionCodes.PatternValidated => "Pattern validé par cassure",
                ConfidenceCriterionCodes.InvalidationNotTriggered => "Niveau d'invalidation non franchi",
                _ => criterionCode
            };
        }

        public string GetActionStepLabel(ActionStepKind kind)
        {
            return kind switch
            {
                ActionStepKind.NoteLevel => "Noter le niveau d'invalidation",
                ActionStepKind.SetAlert => "Créer une alerte de niveau",
                ActionStepKind.ReviewAt => "Planifier une revue de l'analyse",
                ActionStepKind.HoldingReminder => "Revoir votre position détenue",
                ActionStepKind.WaitForData => "Attendre des données suffisantes",
                _ => string.Empty
            };
        }
    }
}
