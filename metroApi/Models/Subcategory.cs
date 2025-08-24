namespace metroApi.Models
{
    public class Subcategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g., "LED Cooler Lights"

        public int CategoryId { get; set; } // Foreign key
        public Category? Category { get; set; } // Navigation property

        // Navigation property for products
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}