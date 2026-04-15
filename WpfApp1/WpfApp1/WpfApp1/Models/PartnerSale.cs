using System;

namespace WpfApp1.Models
{
    public sealed class PartnerSale
    {
        public string SaleId { get; set; }
        public int PartnerId { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime SaleDate { get; set; }
        public string ProductName { get; set; }
        public string ProductTypeName { get; set; }
        public string PartnerName { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
