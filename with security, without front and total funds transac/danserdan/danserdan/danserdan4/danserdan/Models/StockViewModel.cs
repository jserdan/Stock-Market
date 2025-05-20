using System.Collections.Generic;
using danserdan.Services;

namespace danserdan.Models
{
    public class StockViewModel
    {
        public int Id { get; set; }
        public required string Symbol { get; set; }
        public required string Name { get; set; }
        public required string Sector { get; set; }
        public required string Price { get; set; }
        public required string Change { get; set; }
        public required string ChangeClass { get; set; }
        public required string Color { get; set; }
        public required string Hour1 { get; set; }
        public required string Hour24 { get; set; }
        public required string Days7 { get; set; }
        public required string Hour1Class { get; set; }
        public required string Hour24Class { get; set; }
        public required string Days7Class { get; set; }
        public required List<ChartDataset> ChartData { get; set; } = new List<ChartDataset>();
    }
}
