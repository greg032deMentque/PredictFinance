namespace BackPredictFinance.Common.enums
{
    public enum CriterionState
    {
        Met,
        Partial,
        Absent
    }

    public enum CriterionSource
    {
        Detection,
        Validation,
        Invalidation
    }

    public enum ActionStepKind
    {
        NoteLevel,
        ReviewAt,
        SetAlert,
        HoldingReminder,
        WaitForData
    }

    // Partagé avec les notifications/alertes proactives (cf. 05 §2.9, glossaire §8 ; futur C-10).
    public enum AlertTrigger
    {
        PatternStateChange,
        LevelCrossed,
        DataStale
    }
}
