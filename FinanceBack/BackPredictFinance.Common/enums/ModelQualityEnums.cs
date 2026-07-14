namespace BackPredictFinance.Common.enums
{
    public enum ModelStatusEnum
    {
        NoGo = 0,
        Go = 1
    }

    public enum ModelCheckEnum
    {
        Precision = 0,
        F1 = 1,
        RocAuc = 2,
        MinimumPositives = 3
    }

    public enum ModelCheckStatusEnum
    {
        Fail = 0,
        Pass = 1,
        NotApplicable = 2
    }
}
