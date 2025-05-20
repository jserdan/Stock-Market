using System;
using System.ComponentModel.DataAnnotations;

namespace danserdan.Models
{
    public class PaymentRequest
    {
        [Required]
        public decimal amount { get; set; }
    }
}
