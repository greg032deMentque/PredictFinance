namespace BackPredictFinance.Datas.Entities
{
    public class Analytic
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Login { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public string Request { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Referer { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
    }
}
