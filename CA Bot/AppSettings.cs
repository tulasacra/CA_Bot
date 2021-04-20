namespace CA_Bot
{
    public class AppSettings
    {
        public string AccessID { get; set; }
        public string SecretKey { get; set; }
        public string WithdrawalAddress { get; set; }

        public decimal SourceDailyAmount { get; set; }
    }
}