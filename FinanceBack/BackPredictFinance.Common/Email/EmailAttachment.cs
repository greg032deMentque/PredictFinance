namespace BackPredictFinance.Common.Email
{
    public sealed class EmailAttachment
    {
        public string FileName { get; init; } = string.Empty;
        public byte[] Content { get; init; } = Array.Empty<byte>();
        public string? ContentType { get; init; }
    }
}
