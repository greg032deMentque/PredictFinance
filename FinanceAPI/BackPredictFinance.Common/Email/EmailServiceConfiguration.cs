namespace BackPredictFinance.Common.Email
{
	public class EmailServiceConfiguration
	{
		public string From { get; set; }
		public List<string> To { get; set; }
		public string SmtpServer { get; set; }
		public int Port { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
	}
}
