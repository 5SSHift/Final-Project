namespace Wpf.Models
{
    public class Product
    {
        public int      Id           { get; set; }
        public string   Name         { get; set; } = string.Empty;
        public string   Description  { get; set; } = string.Empty;
        public decimal  Price        { get; set; }
        public int      Stock        { get; set; }
        public DateTime CreatedAt    { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string   Category     { get; set; } = string.Empty;
        public byte[]?  ImageData    { get; set; }
        public decimal  DiscountPercentage { get; set; } = 0;
        public bool     IsOnOffer    { get; set; } = false;

        /// <summary>Prețul final calculat — folosit în UI pentru binding direct.</summary>
        public decimal FinalPrice
            => IsOnOffer && DiscountPercentage > 0
               ? Math.Round(Price * (1 - DiscountPercentage / 100m), 2)
               : Price;

        public decimal GetDiscountedPrice() => FinalPrice;
    }
}
