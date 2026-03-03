namespace BackPredictFinance.Datas.Entities
{
    public class Analytic
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string Login { get; set; }
		public string Ip { get; set; }
		public string Request { get; set; }
		public string Body { get; set; }
		public DateTime Date { get; set; }
		public string Referer { get; set; }
		public string UserAgent { get; set; }
	}
}

