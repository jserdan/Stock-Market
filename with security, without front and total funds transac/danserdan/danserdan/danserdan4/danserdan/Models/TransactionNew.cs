using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace danserdan.Models
{
    [Table("transactions")]
    public class Transaction
    {
        [Key]
        [Column("transaction_id")]
        public int transaction_id { get; set; }
        
        [Required]
        [Column("user_id")]
        public int user_id { get; set; }
        
        [ForeignKey("user_id")]
        public Users? User { get; set; }
        
        [Column("stock_id")]
        public int? StockId { get; set; }
        
        [Required]
        [Column("quantity")]
        public int quantity { get; set; }
        
        [Required]
        [Column("price", TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        [Required]
        [Column("transaction_time")]
        public DateTime TransactionTime { get; set; }
        
        [Column("transaction_type")]
        public string? TransactionType { get; set; }
    }
}
