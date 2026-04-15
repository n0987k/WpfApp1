namespace WpfApp1.Models
{
    public sealed class CatalogColumnInfo
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public int? CharacterMaxLength { get; set; }
        public bool IsNullable { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsComputed { get; set; }
        public bool IsPrimaryKey { get; set; }
    }
}
