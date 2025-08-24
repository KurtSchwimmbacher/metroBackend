namespace metroApi.Models
{
    public class Cart
    {
        public int Id { get; set; }

        public int UserId { get; set; } // Foreign key
        public User? User { get; set; } // Navigation property

        // Navigation property for items
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}