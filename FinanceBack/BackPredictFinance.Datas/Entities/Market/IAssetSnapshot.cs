namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Contrat commun aux snapshots horodatés rattachés à un actif (cotation, fondamentaux, etc.),
    /// utilisé pour factoriser la sélection du dernier snapshot par actif.
    /// </summary>
    public interface IAssetSnapshot
    {
        string AssetId { get; set; }
        DateTime AsOfUtc { get; set; }
    }
}
