namespace BackPredictFinance.Common.enums
{
    public enum PeaEligibilityStatusEnum
    {
        Unknown = 0,
        ConfirmedEligible = 1,
        ConfirmedIneligible = 2
    }

    public enum PeaEligibilitySourceTypeEnum
    {
        Unknown = 0,
        ManualRegistry = 1,
        BrokerConfirmation = 2,
        IssuerReference = 3,
        ExchangeReference = 4
    }
}
