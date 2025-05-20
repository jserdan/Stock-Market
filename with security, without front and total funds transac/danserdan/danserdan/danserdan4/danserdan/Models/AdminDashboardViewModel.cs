namespace danserdan.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalStocks { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalTransactions { get; set; }
        public Dictionary<string, decimal> MonthlyData { get; set; } = new Dictionary<string, decimal>();
        public List<object> RecentTransactions { get; set; } = new List<object>();
    }
}
