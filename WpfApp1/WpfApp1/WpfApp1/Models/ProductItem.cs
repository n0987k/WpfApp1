namespace WpfApp1.Models
{
    public sealed class ProductItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int? ProductTypeId { get; set; }
        public decimal? Price { get; set; }
        public string ArticleNumber { get; set; }
    }
}
