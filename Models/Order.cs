namespace Wpf.Models
{
    public class Order
    {
        public int      OrderID         { get; set; }
        public int      Client_Id       { get; set; }
        public DateTime OrderDate       { get; set; } = DateTime.UtcNow;
        public string   Status          { get; set; } = "Pending";
        public decimal  TotalAmount     { get; set; }
        public string   ShippingAddress { get; set; } = string.Empty;
        public string   PaymentMethod   { get; set; } = string.Empty;
        public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;

        // Navigation (joined)
        public string   ClientUsername  { get; set; } = string.Empty;

        /// <summary>
        /// Valoare numerică de sortare: Pending=1, Processing=2, Shipped=3, Delivered=4, Cancelled=5.
        /// Permite ICollectionView să sorteze corect pe click pe coloana Status.
        /// </summary>
        public int StatusOrder => Status switch
        {
            "Pending"    => 1,
            "Processing" => 2,
            "Shipped"    => 3,
            "Delivered"  => 4,
            "Cancelled"  => 5,
            _            => 6
        };
    }
}
