namespace Wpf.Models
{
    public class OrderDetail
    {
        public int     OrderDetailID { get; set; }
        public int     OrderID       { get; set; }
        public int     Product_Id    { get; set; }
        public int     Quantity      { get; set; }
        public decimal UnitPrice     { get; set; }

        // Navigation (joined)
        public string  ProductName   { get; set; } = string.Empty;
    }
}
