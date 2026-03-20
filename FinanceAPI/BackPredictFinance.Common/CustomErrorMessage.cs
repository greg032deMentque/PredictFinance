namespace BackPredictFinance.Common
{
    public class CustomErrorMessage
    {
        public string DateTime { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string Exception { get; set; } = string.Empty;
        public string Source_ip { get; set; } = string.Empty;
        public string Host_ip { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string protocol { get; set; } = string.Empty;
        public int? Port { get; set; }
        public string Request_uri { get; set; } = string.Empty;
        public string Request_method { get; set; } = string.Empty;
        public string Trace { get; set; } = string.Empty;
        public string CurrentUserId { get; set; } = string.Empty;
    }

}
