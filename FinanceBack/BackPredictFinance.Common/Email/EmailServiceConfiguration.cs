namespace BackPredictFinance.Common.Email
{
    public class EmailServiceConfiguration
    {
        public string From { get; set; } = string.Empty;
        public List<string> To { get; set; } = [];
        public string SmtpServer { get; set; } = string.Empty;
        public int Port { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromName { get; set; } = "PredictFinance";
    }
}
