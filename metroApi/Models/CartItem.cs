namespace metroApi.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; } = 1;

        public int ProductId { get; set; } // Foreign key
        public Product? Product { get; set; } // Navigation property

        public int CartId { get; set; } // Foreign key
        public Cart? Cart { get; set; } // Navigation property
    }
}