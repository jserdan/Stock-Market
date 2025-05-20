using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace danserdan.Models
{
    public class Stocks
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int stock_id { get; set; }

        [Required]
        public required string symbol { get; set; }

        [Required]
        public required string company_name { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal market_price { get; set; }

        public DateTime last_updated { get; set; }

        // Added for daily open price tracking
        [Column(TypeName = "decimal(18,2)")]
        public decimal? open_price { get; set; }
        public DateTime? open_price_time { get; set; }
        
        // Flag to track if stock is available for trading
        public bool IsAvailable { get; set; } = true;
        
        // Stock sector for categorization
        public string? sector { get; set; }
    }
}