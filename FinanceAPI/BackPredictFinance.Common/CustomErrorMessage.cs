namespace BackPredictFinance.Common
{
    public class CustomErrorMessage
    {
        public string DateTime { get; set; }
        public int StatusCode { get; set; }
        public string Exception { get; set; }
        public string Source_ip { get; set; }
        public string Host_ip { get; set; }
        public string Hostname { get; set; }
        public string protocol { get; set; }
        public int? Port { get; set; }
        public string Request_uri { get; set; }
        public string Request_method { get; set; }
        public string Trace { get; set; }
        public string CurrentUserId { get; set; }
    }

}
