using System;
using System.Collections.Generic;

namespace danserdan.Models
{
    public class StockHolding
    {
        public int StockId { get; set; }
        public string Symbol { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal ProfitLoss { get; set; }
        public string ProfitLossPercentage { get; set; } = "0.00%";
    }

    public class ProfileViewModel
    {
        public required Users User { get; set; }
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
        public List<StockHolding> StockHoldings { get; set; } = new List<StockHolding>();
        public int TotalTrades { get; set; }
        public int UniqueStocks { get; set; }
        public decimal TotalReturn { get; set; }
        public required string ReturnPercentage { get; set; } = "0.00%";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 5;
    }
}
